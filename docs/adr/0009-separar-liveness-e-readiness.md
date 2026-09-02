# ADR 0009 - Separar liveness e readiness

## Status

Aceita

## Contexto

Em ambientes com containers, orquestradores ou balanceadores, uma aplicação pode estar viva, mas ainda não estar pronta para receber tráfego.

No caso desta solução, uma API pode estar executando, mas ainda não estar pronta para sua responsabilidade principal. A API de lançamentos precisa do PostgreSQL para gravar. A API de consolidação precisa do PostgreSQL para consultar a fonte da verdade dos saldos e usa Redis como cache de leitura. O RabbitMQ é desacoplado da escrita pela Outbox.

## Decisão

Separar as verificações de saúde em três endpoints:

| Endpoint | Uso |
| --- | --- |
| `GET /health` | Compatibilidade e verificação básica da API consultada. |
| `GET /health/live` | Indica se o processo da API consultada está vivo. |
| `GET /health/ready` | Indica se a API consultada está pronta para operar com suas dependências. |

No readiness da API de lançamentos:

- PostgreSQL é dependência crítica, porque sem ele a API não consegue persistir lançamentos;
- RabbitMQ é dependência não crítica, porque a Outbox permite registrar eventos pendentes e publicar depois;
- no modo de armazenamento em arquivo, a dependência crítica é o armazenamento local.

No readiness da API de consolidação:

- PostgreSQL é dependência crítica, porque sem ele a API não consegue consultar a fonte da verdade dos saldos;
- Redis é dependência não crítica, porque a API pode consultar PostgreSQL se o cache falhar;
- no modo de armazenamento em arquivo, a dependência crítica é o armazenamento local.

Se uma dependência crítica falhar, o endpoint `/health/ready` retorna `503 Service Unavailable`. Se apenas dependências não críticas falharem, retorna `200 OK` com status `Degraded`.

## Consequências Positivas

- Melhora a leitura operacional da solução.
- Evita reiniciar a API apenas porque uma dependência não crítica falhou temporariamente.
- Explica a decisão de resiliência ligada a Redis e RabbitMQ.
- Prepara a solução para uso futuro com orquestradores e balanceadores.

## Consequências Negativas

- Aumenta a quantidade de contratos operacionais expostos pela API.
- Exige documentação clara para evitar confundir liveness com readiness.
- Ainda não substitui métricas, tracing e dashboards completos.

## Alternativas Consideradas

### Manter apenas `GET /health`

Foi descartada porque mistura disponibilidade do processo com prontidão operacional.

### Tornar RabbitMQ crítico para readiness

Foi descartada porque contraria a decisão de usar Outbox. A API deve continuar criando lançamentos mesmo se o RabbitMQ estiver temporariamente indisponível.

### Tornar Redis crítico para readiness

Foi descartada porque o Redis é cache de leitura. A fonte da verdade permanece no PostgreSQL.

## Evolução Futura

Em uma evolução produtiva, os health checks podem avançar para:

- health check do worker de consolidação;
- health check separado para Outbox pendente ou com falha;
- health checks nativos do orquestrador;
- métricas de latência por dependência;
- dashboards e alertas por status `Healthy`, `Degraded` e `Unhealthy`.
