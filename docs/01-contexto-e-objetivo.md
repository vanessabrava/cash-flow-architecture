# Contexto e Objetivo

## Contexto do Problema

Pequenos comerciantes realizam movimentações financeiras diariamente e precisam controlar entradas e saídas de dinheiro de forma simples, confiável e consultável.

Essas movimentações são representadas por lançamentos financeiros, que podem ser de dois tipos principais:

- Crédito: entrada de valor no fluxo de caixa.
- Débito: saída de valor do fluxo de caixa.

Além do registro dos lançamentos, o comerciante precisa consultar o saldo consolidado por dia, considerando todos os créditos e débitos daquele período.

## Objetivo da Solução

Propor uma arquitetura que permita:

- Registrar lançamentos financeiros de crédito e débito.
- Consultar lançamentos registrados.
- Consolidar o saldo diário.
- Consultar o saldo diário consolidado.
- Manter o serviço de lançamentos disponível mesmo se o serviço de consolidação estiver indisponível.

## Escopo Atual

O repositório já contém documentação arquitetural, decisões registradas em ADRs, implementação inicial da API, persistência em PostgreSQL, publicação de eventos no RabbitMQ, worker assíncrono de consolidação, Docker Compose e pipeline de CI.

## Fora do Escopo Atual

Para manter a evolução do projeto organizada, o escopo atual ainda não cobre:

- Autenticação e autorização.
- Padrão Outbox para garantir atomicidade entre gravação no banco e publicação de evento.
- Fila de erro para mensagens que falharem repetidamente.
- Observabilidade completa com dashboards, métricas e tracing distribuído.
- Testes de carga.
- Deploy em ambiente cloud.
