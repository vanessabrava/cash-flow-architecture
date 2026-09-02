# ADR 0006 - Usar Redis para cache de saldo diário

## Status

Aceita

## Contexto

A consulta de saldo diário consolidado tende a ser uma operação de leitura frequente. Como o saldo consolidado é uma visão derivada dos lançamentos, ele já fica persistido no PostgreSQL para evitar recalcular o histórico a cada consulta.

Mesmo assim, consultas repetidas para a mesma data podem gerar carga desnecessária no banco. Um cache de leitura ajuda a reduzir latência e aliviar a base relacional, principalmente em cenários de pico.

## Decisão

Usar Redis como cache de leitura para o endpoint `GET /daily-balances/{date}`, mantendo o PostgreSQL como fonte da verdade.

A atualização principal do cache acontece quando a consolidação processa um evento e atualiza o saldo diário. A consulta pela API usa Redis primeiro e recorre ao PostgreSQL quando o cache não possui a data solicitada ou quando o Redis está indisponível.

Regras da decisão:

- o PostgreSQL permanece como fonte da verdade;
- o Redis armazena apenas respostas de saldo consolidado;
- saldos pendentes não são cacheados nesta etapa;
- o worker de consolidação atualiza o Redis após consolidar o saldo no PostgreSQL;
- o endpoint manual `POST /daily-balances/process-events` também atualiza o Redis enquanto existir como apoio de desenvolvimento;
- a API consulta Redis antes de consultar PostgreSQL;
- o cache usa TTL maior, configurado inicialmente em 15 minutos;
- o TTL existe como proteção operacional contra cache antigo preso, não como mecanismo principal de atualização;
- falha no Redis não deve indisponibilizar a API;
- se o Redis estiver indisponível, a API consulta o PostgreSQL diretamente.

## Consequências Positivas

- Reduz leituras repetidas no PostgreSQL.
- Melhora o tempo de resposta em consultas frequentes da mesma data.
- Representa uma decisão arquitetural comum em cenários de leitura intensiva.
- Mantém independência entre cache e persistência principal.
- Mantém o cache mais próximo do saldo recém-consolidado, porque a atualização ocorre no momento da consolidação.

## Consequências Negativas

- Introduz mais uma dependência operacional.
- Pode retornar uma resposta defasada se a atualização do PostgreSQL funcionar e a atualização do Redis falhar.
- Exige observabilidade específica para taxa de acerto, erro e latência do cache.
- Exige estratégia futura de invalidação e retry caso os requisitos de frescor aumentem.

## Alternativas Consideradas

### Consultar apenas o PostgreSQL

É a alternativa mais simples e continua sendo segura para o volume inicial. Foi evoluída nesta etapa porque o Redis já fazia sentido no desenho de arquitetura e enriquece a solução do desafio.

### Atualizar cache apenas pela API de consulta

Foi descartado como estratégia principal porque poderia deixar o Redis desatualizado até a primeira consulta após a consolidação. A API ainda pode gravar no cache quando buscar um saldo consolidado no PostgreSQL, mas a atualização principal fica ligada ao processamento da consolidação.

### Não usar TTL

Foi descartado porque uma falha entre atualizar PostgreSQL e atualizar Redis poderia manter um saldo antigo indefinidamente. O TTL de 15 minutos limita esse risco sem transformar expiração em mecanismo principal de consistência.

### Cachear também respostas pendentes

Foi descartado nesta etapa porque poderia prolongar artificialmente o estado `PENDING` logo após a consolidação ser concluída.

## Evolução Futura

Em uma solução produtiva, a estratégia de cache pode evoluir para:

- métricas de hit, miss, erro e latência;
- health check de Redis separado de liveness;
- invalidação do cache após consolidação;
- retry específico para falha de atualização do cache;
- evento dedicado de atualização de projeção, caso a atualização direta pelo worker deixe de ser suficiente;
- fallback com circuit breaker caso o Redis apresente instabilidade.
