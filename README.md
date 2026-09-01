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
    ├── adr
    │   ├── 0001-separar-lancamentos-e-consolidacao.md
    │   └── 0002-processar-consolidacao-de-forma-assincrona.md
    ├── 01-contexto-e-objetivo.md
    ├── 02-requisitos-iniciais.md
    ├── 03-premissas-restricoes-e-decisoes.md
    └── 04-arquitetura-logica.md
```

## Documentação

- [Contexto e objetivo](docs/01-contexto-e-objetivo.md)
- [Requisitos iniciais](docs/02-requisitos-iniciais.md)
- [Premissas, restrições e decisões](docs/03-premissas-restricoes-e-decisoes.md)
- [Arquitetura lógica](docs/04-arquitetura-logica.md)
- [ADR 0001 - Separar lançamentos e consolidação](docs/adr/0001-separar-lancamentos-e-consolidacao.md)
- [ADR 0002 - Processar consolidação de forma assíncrona](docs/adr/0002-processar-consolidacao-de-forma-assincrona.md)

## Idioma do Projeto

A documentação do projeto será mantida em português. Quando houver código, nomes técnicos de código, APIs, classes, métodos, serviços e variáveis serão escritos em inglês.

## Próximas Etapas

As próximas entregas devem evoluir o repositório em partes pequenas e commitáveis, por exemplo:

1. Refinar requisitos funcionais e não funcionais.
2. Definir contratos de API.
3. Criar a base inicial de implementação.
