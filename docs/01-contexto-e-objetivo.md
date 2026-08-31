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

## Escopo Inicial

Nesta primeira etapa, o repositório contém apenas a base documental do desafio. A implementação, os diagramas, as decisões arquiteturais detalhadas e a modelagem técnica serão adicionados em etapas futuras.

## Fora do Escopo Nesta Etapa

Para manter a evolução do projeto organizada, esta etapa ainda não cobre:

- Implementação de APIs.
- Escolha final de tecnologias.
- Criação de banco de dados.
- Mensageria ou processamento assíncrono.
- Diagramas de arquitetura.
- Scripts de infraestrutura.
- Pipeline de CI/CD.
