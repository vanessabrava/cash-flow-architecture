# Resiliência e Observabilidade

Este documento descreve a estratégia inicial para lidar com falhas, monitorar a solução e manter rastreabilidade das operações principais.

## Objetivo

A solução deve continuar aceitando lançamentos financeiros mesmo quando a consolidação diária estiver indisponível ou atrasada. Também deve oferecer sinais suficientes para identificar falhas, acompanhar o processamento e reprocessar informações quando necessário.

## Cenários de Falha

| Cenário | Impacto | Estratégia |
| --- | --- | --- |
| Falha temporária no processador de consolidação | Novos lançamentos continuam sendo registrados, mas o saldo pode ficar atrasado. | Manter eventos pendentes para processamento posterior. |
| Falha temporária no RabbitMQ durante criação de lançamento | Lançamento não deve ser perdido nem rejeitado apenas por falha momentânea da mensageria. | Gravar evento na Outbox do PostgreSQL e publicar posteriormente com retentativas controladas. |
| Falha ao atualizar saldo consolidado | Saldo da data pode ficar desatualizado ou com status de falha. | Registrar erro, permitir nova tentativa e preservar os lançamentos originais. |
| Duplicidade no processamento de evento | Saldo pode ser calculado incorretamente se o evento for aplicado mais de uma vez. | Prever processamento idempotente na consolidação. |
| Retry duplicado na criação de lançamento | O mesmo lançamento pode ser registrado mais de uma vez por timeout, clique duplicado ou retry automático. | Aceitar `Idempotency-Key` no `POST /entries` para reaproveitar o resultado da primeira criação. |
| Requisição sem autenticação | Consumidor não autorizado pode tentar acessar endpoints de negócio. | Exigir `X-Api-Key` nos endpoints protegidos e registrar tentativas negadas sem expor a chave. |
| Redis indisponível | Consulta de saldo pode perder ganho de performance. | Tratar cache como dependência opcional e consultar PostgreSQL diretamente em caso de falha. |
| Cache defasado | Consulta pode retornar saldo consolidado anterior se a atualização do Redis falhar após a consolidação no PostgreSQL. | Atualizar o Redis no processamento da consolidação e manter TTL de 15 minutos como proteção operacional. |
| Lentidão no canal de eventos | Atraso entre lançamento e saldo consolidado. | Monitorar fila, atraso de consumo e volume pendente. |
| Erro de validação no lançamento | Lançamento não deve ser registrado. | Retornar erro claro para o consumidor da API. |

## Estratégia de Resiliência

### Desacoplamento

A API de Lançamentos deve persistir o lançamento e registrar um evento de integração para a consolidação. A resposta ao comerciante não deve depender do cálculo do saldo consolidado nem da disponibilidade imediata do RabbitMQ.

### Outbox

A criação do lançamento grava o evento `EntryCreated` na tabela `outbox_messages` dentro do PostgreSQL. Uma rotina em segundo plano publica mensagens pendentes no RabbitMQ e marca `processedAt` após sucesso.

Essa abordagem reduz a janela de falha entre salvar o lançamento e publicar o evento.

Quando a publicação falha, a mensagem recebe incremento em `retryCount` e uma nova tentativa é agendada em `nextAttemptAt`. Após atingir o limite configurado de tentativas, a mensagem recebe `failedAt` e deixa de ser republicada automaticamente.

### Reprocessamento

A consolidação deve poder ser reprocessada em caso de falha. O reprocessamento pode usar:

- eventos ainda não processados;
- eventos enviados para uma fila de erro;
- lançamentos originais armazenados na base de lançamentos.

### Idempotência

O processamento da consolidação deve ser idempotente. Isso significa que processar o mesmo evento mais de uma vez não deve gerar saldo duplicado.

Uma estratégia possível é registrar quais eventos já foram aplicados ou recalcular o saldo diário a partir da fonte principal de lançamentos.

A criação de lançamentos também deve oferecer idempotência opcional por meio do header `Idempotency-Key`.

Essa idempotência protege a operação contra duplicidade acidental causada por retry do consumidor. Ela não deve impedir lançamentos legítimos com mesmo tipo, valor, descrição e data. A chave de idempotência representa a tentativa lógica de criação, não o conteúdo financeiro do lançamento.

Regras esperadas:

- requisições sem `Idempotency-Key` criam um novo lançamento a cada `POST`, mesmo que o payload seja igual;
- requisições com a mesma `Idempotency-Key` para a mesma operação retornam o lançamento já criado;
- reutilizar a mesma chave com payload diferente deve ser tratado como erro de conflito;
- registros de idempotência devem ter retenção limitada para evitar crescimento indefinido.

### Consistência Eventual

Como a consolidação é assíncrona, o saldo consultado pode não refletir imediatamente um lançamento recém-criado.

Esse comportamento deve ser tratado como parte da arquitetura, e não como erro, desde que o atraso seja monitorado e permaneça dentro de limites aceitáveis.

### Cache de Leitura

A consulta de saldo diário consolidado usa Redis como cache de leitura. A API de Consulta de Saldo tenta ler o saldo no cache antes de consultar o PostgreSQL.

A atualização principal do cache acontece no processamento de consolidação: depois de aplicar o evento no PostgreSQL, o worker grava o saldo atualizado no Redis. O endpoint manual de processamento também atualiza o cache enquanto existir como apoio de desenvolvimento.

Quando a API de Consulta de Saldo não encontra o saldo no Redis e encontra o saldo consolidado no PostgreSQL, ela grava uma cópia no cache. Esse comportamento cobre cache miss e recriação do cache após reinício do Redis.

O cache não é fonte da verdade. Se o Redis falhar, a API de Consulta de Saldo registra o problema e consulta o PostgreSQL diretamente.

O TTL inicial é de 15 minutos. Ele existe para limitar o risco de cache antigo preso em caso de falha operacional, não para ser o mecanismo principal de atualização do saldo.

## Logs

Os logs devem permitir rastrear as principais operações da solução.

As APIs possuem middleware de logs HTTP estruturados. Ele obtém ou gera o `correlationId` no início da requisição, grava esse valor no header de resposta `X-Correlation-Id`, usa o mesmo valor no `TraceIdentifier` e registra método HTTP, rota, status code e duração.

APIs e worker escrevem logs no console em formato JSON, com timestamp em UTC e scopes habilitados. Logs verbosos de infraestrutura em nível `Information`, como comandos SQL do EF Core, são filtrados para reduzir ruído durante a execução local.

Payloads, senhas, API Keys e dados sensíveis não devem ser registrados.

| Operação | Informações Relevantes |
| --- | --- |
| Requisição HTTP | `correlationId`, método, rota, status code e duração. |
| Criação de lançamento | `correlationId`, `entryUid`, tipo, valor, data de referência e momento da criação. |
| Reutilização de chave de idempotência | `correlationId`, `idempotencyKey`, operação, `entryUid` retornado e resultado da reutilização. |
| Publicação de evento | `correlationId`, `eventUid`, `entryUid`, tipo do evento e momento da publicação. |
| Publicação de mensagem da Outbox | `correlationId`, `eventUid`, tipo do evento, tentativa, erro, estado final e resultado da publicação. |
| Processamento de consolidação | `correlationId`, `eventUid`, `entryUid`, data consolidada e resultado do processamento. |
| Falha de consolidação | `correlationId`, `eventUid`, `entryUid`, motivo da falha e tentativa de processamento. |
| Consulta de saldo | `correlationId`, data solicitada, status do saldo e momento da consulta. |
| Falha de autenticação | `correlationId`, rota, método HTTP e status retornado, sem registrar o valor da API Key. |
| Falha de cache | `correlationId`, chave lógica do cache, operação de leitura ou escrita e fallback usado. |

Os logs não devem expor dados sensíveis desnecessários nem identificadores internos do banco.

## Correlation ID

O `correlationId` deve ser propagado entre os componentes para permitir rastreabilidade ponta a ponta.

Quando uma requisição chegar sem `X-Correlation-Id`, a API deve gerar um novo valor. Quando o header for informado, a solução deve reaproveitar esse valor nos logs, respostas HTTP e eventos publicados.

Exemplo de jornada rastreável:

1. Consumidor envia `POST /entries` com `X-Correlation-Id`.
2. API de Lançamentos registra o lançamento e grava logs com o mesmo `correlationId`.
3. API publica o evento `EntryCreated` contendo o mesmo `correlationId`.
4. Processador de Consolidação consome o evento e grava logs com o mesmo `correlationId`.
5. Em caso de falha, o suporte consegue consultar logs relacionados à mesma jornada.

## Métricas

Métricas sugeridas para acompanhar a saúde da solução:

| Métrica | Objetivo |
| --- | --- |
| Quantidade de lançamentos criados | Acompanhar volume de uso da API de Lançamentos. |
| Tempo de resposta da criação de lançamento | Identificar lentidão no fluxo principal. |
| Quantidade de eventos publicados | Validar se lançamentos estão gerando eventos. |
| Quantidade de mensagens pendentes na Outbox | Identificar eventos ainda não publicados no RabbitMQ. |
| Quantidade de falhas de publicação da Outbox | Identificar indisponibilidade ou instabilidade da mensageria. |
| Quantidade de mensagens com falha definitiva na Outbox | Identificar eventos que exigem análise ou reprocessamento administrativo. |
| Quantidade de eventos pendentes | Acompanhar acúmulo no processamento assíncrono. |
| Tempo médio de consolidação | Medir atraso entre lançamento e saldo consolidado. |
| Quantidade de falhas de consolidação | Identificar instabilidade no processamento. |
| Quantidade de reprocessamentos | Avaliar necessidade de correção operacional. |
| Requisições sem `correlationId` recebido | Avaliar maturidade dos consumidores e necessidade de geração interna. |
| Reutilização de `Idempotency-Key` | Acompanhar retries e possíveis problemas de comunicação com consumidores. |
| Conflitos de `Idempotency-Key` | Identificar reutilização incorreta da mesma chave para payload diferente. |
| Respostas `401 Unauthorized` | Acompanhar tentativas sem autenticação ou com credenciais inválidas. |
| Taxa de acerto do cache | Medir efetividade do Redis nas consultas de saldo. |
| Falhas de leitura ou escrita no cache | Identificar instabilidade do Redis sem confundir com falha da API. |

## Health Checks

Cada componente deve expor verificações básicas de saúde.

| Componente | Verificações |
| --- | --- |
| API de Lançamentos | Liveness da aplicação e readiness com PostgreSQL como dependência crítica. |
| API de Consulta de Saldo | Liveness da aplicação e readiness com PostgreSQL como dependência crítica. Redis deve aparecer como dependência não crítica. |
| Processador de Consolidação | Aplicação disponível, conexão com canal de eventos e conexão com base de saldos. |

Endpoints da API:

| Endpoint | Objetivo |
| --- | --- |
| `GET /health` | Verificação básica e compatibilidade com validações locais. |
| `GET /health/live` | Indicar se o processo da API consultada está vivo. |
| `GET /health/ready` | Indicar se a API consultada está pronta para operar com dependências críticas. |

No readiness da API de Lançamentos, RabbitMQ é reportado como dependência não crítica porque a publicação de eventos é protegida pela Outbox. No readiness da API de Consulta de Saldo, Redis é reportado como dependência não crítica porque a consulta possui fallback para PostgreSQL.

## Alertas

Alertas devem ser considerados para situações que afetam a operação:

- falha contínua no processador de consolidação;
- aumento anormal de eventos pendentes;
- aumento anormal de mensagens pendentes na Outbox;
- mensagens da Outbox paradas por muito tempo sem `processedAt`;
- mensagens da Outbox com `failedAt` preenchido;
- atraso elevado entre criação do lançamento e consolidação;
- erro recorrente ao persistir saldo consolidado;
- readiness de alguma API retornando `Unhealthy`;
- readiness de alguma API retornando `Degraded` por tempo prolongado;
- indisponibilidade da API de Lançamentos;
- aumento anormal de respostas `401 Unauthorized`;
- aumento de falhas de leitura ou escrita no Redis;
- aumento de respostas `500 Internal Server Error`.

## Relação com Requisitos Não Funcionais

Esta estratégia apoia principalmente os seguintes requisitos:

| Requisito | Como é atendido |
| --- | --- |
| RNF-001 | O desacoplamento evita que falhas na consolidação bloqueiem lançamentos. |
| RNF-002 | O processamento assíncrono permite evolução da consolidação. |
| RNF-003 | Logs e métricas ajudam na rastreabilidade. |
| RNF-005 | A separação de responsabilidades reduz acoplamento. |

## Evoluções Para Produção

Os itens abaixo representam aumento de maturidade operacional para produção. Eles não impedem a execução local nem a avaliação arquitetural do desafio:

- Evoluir a separação atual para projetos mais granulares de aplicação, domínio, contratos e infraestrutura específica quando o domínio crescer.
- Evoluir autenticação por API Key local para OAuth2, OpenID Connect, JWT, API Gateway ou identidade serviço-a-serviço.
- Definir autorização por escopo, perfil ou recurso.
- Evoluir retry e observabilidade da atualização de cache após consolidação.
- Evoluir a política da Outbox para backoff exponencial, jitter e reprocessamento administrativo.
- Definir estratégia de fila de erro dedicada.
- Definir limites aceitáveis de atraso na consolidação.
- Padronizar nomes de campos dos logs entre APIs, worker e infraestrutura.
- Definir dashboards e alertas operacionais.
- Criar health checks específicos para o worker de consolidação.

## Ferramentas Locais de Diagnóstico

As ferramentas abaixo existem apenas para desenvolvimento local:

| Ferramenta | Uso |
| --- | --- |
| Adminer | Inspecionar tabelas e registros do PostgreSQL local. |
| RabbitMQ Management | Inspecionar exchange, fila, mensagens e consumidores do RabbitMQ local. |
| Redis Commander | Inspecionar chaves, valores e TTLs do Redis local. |

Essas ferramentas não devem ser expostas em produção sem controles adequados de rede, autenticação, autorização e auditoria.
