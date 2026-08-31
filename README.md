# Cash Flow Architecture

Este repositório documenta uma proposta de arquitetura de solução para uma plataforma de controle de fluxo de caixa, com foco em lançamentos financeiros e consolidação diária de saldo.

O objetivo é organizar a solução de forma progressiva, separando entendimento do problema, requisitos, decisões arquiteturais, desenho técnico e evolução da implementação.

## Contexto

Pequenos comerciantes precisam registrar lançamentos de crédito e débito ao longo do dia e consultar o saldo diário consolidado. A solução deve considerar a continuidade do serviço de lançamentos mesmo quando o processamento de consolidação estiver indisponível.

## Estrutura Inicial

```text
.
├── README.md
└── docs
    ├── 01-contexto-e-objetivo.md
    └── 02-requisitos-iniciais.md
```

## Documentação

- [Contexto e objetivo](docs/01-contexto-e-objetivo.md)
- [Requisitos iniciais](docs/02-requisitos-iniciais.md)

## Idioma do Projeto

A documentação do projeto será mantida em português. Quando houver código, nomes técnicos de código, APIs, classes, métodos, serviços e variáveis serão escritos em inglês.

## Próximas Etapas

As próximas entregas devem evoluir o repositório em partes pequenas e commitáveis, por exemplo:

1. Refinar requisitos funcionais e não funcionais.
2. Definir premissas e restrições da solução.
3. Desenhar a arquitetura lógica.
4. Documentar decisões arquiteturais.
5. Criar a base inicial de implementação.
