# ADR 0004 - Modularizar API, Worker, Core e Infrastructure

## Status

Aceita

## Contexto

A solução possui duas capacidades executáveis principais:

- API de lançamentos e consulta de saldo;
- worker de consolidação assíncrona.

Essas capacidades devem poder evoluir, escalar e falhar de forma independente. Ao mesmo tempo, ambas usam conceitos comuns do domínio, como lançamento financeiro, saldo diário e evento `EntryCreated`.

No início da implementação, o worker chegou a referenciar o projeto da API para reaproveitar código. Essa abordagem funcionava para um incremento inicial, mas deixava um acoplamento conceitual ruim: um serviço de consolidação não deveria depender de um projeto chamado `Api`.

## Decisão

Separar a solution em quatro projetos:

| Projeto | Responsabilidade |
| --- | --- |
| `CashFlowArchitecture.Api` | Expor contratos HTTP, Swagger e endpoints da aplicação. |
| `CashFlowArchitecture.Worker` | Executar o processo assíncrono de consolidação consumindo RabbitMQ. |
| `CashFlowArchitecture.Core` | Concentrar domínio e abstrações compartilhadas entre API, worker e infraestrutura. |
| `CashFlowArchitecture.Infrastructure` | Implementar persistência PostgreSQL, EF Core, mensageria RabbitMQ e armazenamento local de apoio. |

Com essa organização:

- a API não depende do worker;
- o worker não depende da API;
- ambos dependem de contratos de domínio e abstrações no `Core`;
- detalhes técnicos ficam isolados em `Infrastructure`.

## Abordagem Não Aplicada

Não foi adotada uma Clean Architecture completa com múltiplos projetos de `Application`, `Domain`, `Infrastructure`, `Presentation`, `Contracts` e projetos separados por bounded context.

Essa opção não foi aplicada nesta etapa por três motivos:

1. O desafio técnico pede principalmente desenho de solução, disponibilidade, consolidação e evolução arquitetural.
2. A implementação ainda é pequena, e uma divisão excessiva aumentaria o volume de arquivos sem ganho proporcional neste momento.
3. O objetivo é mostrar boa separação de responsabilidades sem transformar o desafio em demonstração de framework arquitetural.

Também não foi feita separação em múltiplos repositórios ou múltiplas soluções. Para o desafio, manter tudo em um monorepo simples facilita revisão, execução local, pipeline e entendimento do avaliador.

## Evolução Futura

Em uma evolução de produto real, a arquitetura poderia avançar para uma separação mais granular:

```text
src/
├── CashFlowArchitecture.Entries.Api
├── CashFlowArchitecture.Balances.Api
├── CashFlowArchitecture.Consolidation.Worker
├── CashFlowArchitecture.Application
├── CashFlowArchitecture.Domain
├── CashFlowArchitecture.Infrastructure.Postgres
├── CashFlowArchitecture.Infrastructure.RabbitMq
└── CashFlowArchitecture.Contracts
```

Nessa evolução:

- a API de lançamentos poderia ficar separada da API de consulta de saldo;
- contratos públicos de eventos e APIs poderiam ficar em um projeto próprio;
- regras de aplicação ficariam separadas das tecnologias de infraestrutura;
- implementações PostgreSQL e RabbitMQ poderiam evoluir de forma independente;
- a publicação de eventos deveria evoluir para Outbox ou mecanismo equivalente.

## Consequências Positivas

- Reduz acoplamento entre API e worker.
- Melhora a leitura arquitetural da solution.
- Facilita explicar independência operacional entre serviços.
- Mantém a implementação proporcional ao tamanho do desafio.
- Prepara o projeto para futuras extrações sem exigir reescrita imediata.

## Consequências Negativas

- Aumenta a quantidade de projetos na solution.
- Exige tornar públicos alguns tipos compartilhados entre assemblies.
- Ainda existe acoplamento entre `Infrastructure` e detalhes de persistência/mensageria no mesmo projeto, aceito por simplicidade nesta etapa.

## Alternativas Consideradas

### Manter o worker referenciando a API

Foi descartada como estrutura final porque cria dependência conceitual inadequada entre dois serviços executáveis.

### Criar uma Clean Architecture completa imediatamente

Foi descartada por ser mais pesada que o necessário para o estágio atual do desafio.

### Separar em múltiplos repositórios

Foi descartada porque dificultaria a avaliação, a execução local e a evolução incremental dentro do escopo do desafio técnico.
