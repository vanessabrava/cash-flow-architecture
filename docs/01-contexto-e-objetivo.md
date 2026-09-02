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

## Escopo Entregue

O repositório contém documentação arquitetural, decisões registradas em ADRs, APIs separadas por responsabilidade, proteção local por API Key, persistência em PostgreSQL, cache de leitura com Redis, publicação confiável de eventos com Outbox, mensageria com RabbitMQ, worker assíncrono de consolidação, Docker Compose e pipeline de CI.

## Evoluções Para Produção

Os itens abaixo não impedem a entrega do desafio. Eles representam evoluções naturais para uma implantação produtiva:

- Autenticação e autorização completas para produção.
- Fila de erro para mensagens que falharem repetidamente.
- Observabilidade completa com dashboards, métricas e tracing distribuído.
- Testes de carga.
- Deploy em ambiente cloud.
