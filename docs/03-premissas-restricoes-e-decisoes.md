# Premissas, Restrições e Decisões

Este documento registra os pontos usados para orientar a arquitetura e acompanhar decisões tomadas durante a evolução incremental do desafio.

## Premissas

Premissas são condições consideradas verdadeiras para permitir a evolução da solução.

| ID | Premissa |
| --- | --- |
| PR-001 | O comerciante precisa registrar lançamentos financeiros ao longo do dia. |
| PR-002 | Um lançamento financeiro pertence a uma data de referência. |
| PR-003 | O saldo diário é calculado a partir da soma dos créditos menos a soma dos débitos de uma data. |
| PR-004 | A consulta do saldo consolidado pode depender de um processamento separado do registro de lançamentos. |
| PR-005 | A indisponibilidade da consolidação não deve impedir novos lançamentos. |

## Restrições

Restrições são limites ou condições que a solução precisa respeitar.

| ID | Restrição |
| --- | --- |
| RE-001 | O serviço responsável por lançamentos deve ser independente do serviço responsável pela consolidação. |
| RE-002 | A arquitetura deve permitir que a consolidação seja reprocessada em caso de falha. |
| RE-003 | A solução deve preservar os lançamentos originais para manter rastreabilidade. |
| RE-004 | A consulta de saldo diário não deve exigir recalcular todo o histórico a cada requisição. |
| RE-005 | A primeira versão da solução deve permanecer simples o suficiente para ser compreendida e evoluída. |

## Decisões Arquiteturais

As decisões abaixo consolidam escolhas já registradas ou implementadas no repositório. Decisões mais relevantes possuem ADR específico.

| ID | Decisão | Justificativa |
| --- | --- | --- |
| DA-001 | Separar o domínio de lançamentos do domínio de consolidação. | Essa separação reduz acoplamento e ajuda a manter lançamentos disponíveis mesmo se a consolidação falhar. |
| DA-002 | Tratar o saldo diário consolidado como uma visão derivada dos lançamentos. | O lançamento é a fonte principal da informação, enquanto o saldo consolidado pode ser recalculado. |
| DA-003 | Usar comunicação assíncrona entre lançamento e consolidação via RabbitMQ. | O processamento assíncrono favorece resiliência e desacoplamento entre os serviços. |
| DA-004 | Persistir dados relacionais em PostgreSQL com EF Core Migrations. | Essa escolha facilita consistência relacional, evolução controlada do schema e execução local em container. |
| DA-005 | Usar `Idempotency-Key` no `POST /entries`. | Essa decisão evita duplicidade acidental causada por retry do consumidor. |
| DA-006 | Separar a solution em `Api`, `Worker`, `Core` e `Infrastructure`. | Essa organização evita dependência direta entre serviços executáveis e mantém a implementação proporcional ao desafio. |

## Pontos em Aberto

Os pontos abaixo permanecem como evolução planejada:

- Estratégia de autenticação.
- Estratégia de autorização.
- Padrão Outbox para publicação confiável de eventos.
- Política de retentativas e fila de erro.
- Estratégia de observabilidade completa.
- Testes de integração com dependências reais em containers.
- Testes de carga para endpoints críticos.

## Riscos Iniciais

| ID | Risco | Mitigação Inicial |
| --- | --- | --- |
| RI-001 | Perda de eventos ou falha no processamento da consolidação. | Prever mecanismo de reprocessamento e rastreabilidade dos lançamentos. |
| RI-002 | Consulta de saldo apresentar informação desatualizada. | Deixar clara a possibilidade de consistência eventual quando houver processamento assíncrono. |
| RI-003 | A solução ficar complexa demais para o objetivo do desafio. | Evoluir em etapas e justificar cada decisão arquitetural. |
