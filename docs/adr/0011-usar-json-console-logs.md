# ADR 0011 - Usar JSON console logs

## Status

Aceita

## Contexto

A API já registra logs HTTP estruturados com método, rota, status, duração e `correlationId`. O worker também registra eventos consumidos e resultado do processamento.

Em execução local com Docker Compose, os logs são lidos principalmente pelo `docker compose logs`. Em ambientes produtivos, logs em console costumam ser coletados por plataformas de observabilidade.

Logs em texto livre são mais fáceis de ler manualmente, mas são piores para consulta, filtro, correlação e criação de alertas.

## Decisão

Configurar API e worker para escrever logs no console em formato JSON usando o provider nativo do .NET.

Regras adotadas:

- logs são emitidos em JSON;
- timestamps usam UTC;
- scopes são incluídos para permitir carregar informações como `CorrelationId`;
- connection strings locais do PostgreSQL desabilitam GSS/Kerberos para evitar mensagens nativas desnecessárias fora do formato JSON;
- logs verbosos de infraestrutura em nível `Information`, como comandos SQL do EF Core, são filtrados para reduzir ruído;
- payloads, senhas, API Keys e dados sensíveis continuam fora dos logs;
- a solução não adiciona ferramenta externa de observabilidade nesta etapa.

## Consequências Positivas

- Facilita ingestão por ferramentas de logs.
- Melhora filtros por propriedades estruturadas.
- Mantém o comportamento aderente a execução em containers.
- Não adiciona dependência externa ao desafio.

## Consequências Negativas

- A leitura manual dos logs fica menos amigável que texto simples.
- Ainda não entrega métricas, tracing distribuído ou dashboards.
- Requer padronização futura de nomes de campos entre API, worker e infraestrutura.

## Alternativas Consideradas

### Manter console text padrão

Foi descartada porque limita a capacidade de busca e correlação em ambientes com múltiplos containers.

### Adotar uma biblioteca externa de logging

Foi considerada, mas deixada para evolução futura. O provider nativo do .NET é suficiente para este estágio do desafio.

### Adicionar stack completa de observabilidade local

Foi descartada nesta etapa para evitar aumentar demais o escopo local. Prometheus, Grafana, Loki, OpenTelemetry Collector ou ferramentas equivalentes podem ser avaliados em uma evolução.

## Evolução Futura

Em uma evolução produtiva, a observabilidade pode avançar para:

- padronização completa de campos;
- tracing distribuído com OpenTelemetry;
- métricas de aplicação e infraestrutura;
- dashboards operacionais;
- alertas por erro, latência, fila, Outbox e readiness degradado.
