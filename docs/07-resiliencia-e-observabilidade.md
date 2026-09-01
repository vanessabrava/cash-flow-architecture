# Resiliência e Observabilidade

Este documento descreve a estratégia inicial para lidar com falhas, monitorar a solução e manter rastreabilidade das operações principais.

## Objetivo

A solução deve continuar aceitando lançamentos financeiros mesmo quando a consolidação diária estiver indisponível ou atrasada. Também deve oferecer sinais suficientes para identificar falhas, acompanhar o processamento e reprocessar informações quando necessário.

## Cenários de Falha

| Cenário | Impacto | Estratégia |
| --- | --- | --- |
| Falha temporária no processador de consolidação | Novos lançamentos continuam sendo registrados, mas o saldo pode ficar atrasado. | Manter eventos pendentes para processamento posterior. |
| Falha ao atualizar saldo consolidado | Saldo da data pode ficar desatualizado ou com status de falha. | Registrar erro, permitir nova tentativa e preservar os lançamentos originais. |
| Duplicidade no processamento de evento | Saldo pode ser calculado incorretamente se o evento for aplicado mais de uma vez. | Prever processamento idempotente na consolidação. |
| Lentidão no canal de eventos | Atraso entre lançamento e saldo consolidado. | Monitorar fila, atraso de consumo e volume pendente. |
| Erro de validação no lançamento | Lançamento não deve ser registrado. | Retornar erro claro para o consumidor da API. |

## Estratégia de Resiliência

### Desacoplamento

A API de Lançamentos deve persistir o lançamento e publicar um evento para a consolidação. A resposta ao comerciante não deve depender do cálculo do saldo consolidado.

### Reprocessamento

A consolidação deve poder ser reprocessada em caso de falha. O reprocessamento pode usar:

- eventos ainda não processados;
- eventos enviados para uma fila de erro;
- lançamentos originais armazenados na base de lançamentos.

### Idempotência

O processamento da consolidação deve ser idempotente. Isso significa que processar o mesmo evento mais de uma vez não deve gerar saldo duplicado.

Uma estratégia possível é registrar quais eventos já foram aplicados ou recalcular o saldo diário a partir da fonte principal de lançamentos.

### Consistência Eventual

Como a consolidação é assíncrona, o saldo consultado pode não refletir imediatamente um lançamento recém-criado.

Esse comportamento deve ser tratado como parte da arquitetura, e não como erro, desde que o atraso seja monitorado e permaneça dentro de limites aceitáveis.

## Logs

Os logs devem permitir rastrear as principais operações da solução.

| Operação | Informações Relevantes |
| --- | --- |
| Criação de lançamento | `correlationId`, `entryUid`, tipo, valor, data de referência e momento da criação. |
| Publicação de evento | `correlationId`, `eventUid`, `entryUid`, tipo do evento e momento da publicação. |
| Processamento de consolidação | `correlationId`, `eventUid`, `entryUid`, data consolidada e resultado do processamento. |
| Falha de consolidação | `correlationId`, `eventUid`, `entryUid`, motivo da falha e tentativa de processamento. |
| Consulta de saldo | `correlationId`, data solicitada, status do saldo e momento da consulta. |

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
| Quantidade de eventos pendentes | Acompanhar acúmulo no processamento assíncrono. |
| Tempo médio de consolidação | Medir atraso entre lançamento e saldo consolidado. |
| Quantidade de falhas de consolidação | Identificar instabilidade no processamento. |
| Quantidade de reprocessamentos | Avaliar necessidade de correção operacional. |
| Requisições sem `correlationId` recebido | Avaliar maturidade dos consumidores e necessidade de geração interna. |

## Health Checks

Cada componente deve expor verificações básicas de saúde.

| Componente | Verificações |
| --- | --- |
| API de Lançamentos | Aplicação disponível e conexão com a base de lançamentos. |
| API de Consulta de Saldo | Aplicação disponível e conexão com a base de saldos consolidados. |
| Processador de Consolidação | Aplicação disponível, conexão com canal de eventos e conexão com base de saldos. |

## Alertas

Alertas devem ser considerados para situações que afetam a operação:

- falha contínua no processador de consolidação;
- aumento anormal de eventos pendentes;
- atraso elevado entre criação do lançamento e consolidação;
- erro recorrente ao persistir saldo consolidado;
- indisponibilidade da API de Lançamentos;
- aumento de respostas `500 Internal Server Error`.

## Relação com Requisitos Não Funcionais

Esta estratégia apoia principalmente os seguintes requisitos:

| Requisito | Como é atendido |
| --- | --- |
| RNF-001 | O desacoplamento evita que falhas na consolidação bloqueiem lançamentos. |
| RNF-002 | O processamento assíncrono permite evolução da consolidação. |
| RNF-003 | Logs e métricas ajudam na rastreabilidade. |
| RNF-005 | A separação de responsabilidades reduz acoplamento. |

## Pontos Para Evolução

- Definir ferramenta de mensageria.
- Definir política de retentativas.
- Definir estratégia de fila de erro.
- Definir limites aceitáveis de atraso na consolidação.
- Definir formato final dos logs estruturados.
- Definir dashboards e alertas operacionais.
