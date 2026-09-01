# Cash Flow Architecture

[![Build and Test](https://github.com/vanessabrava/cash-flow-architecture/actions/workflows/ci.yml/badge.svg)](https://github.com/vanessabrava/cash-flow-architecture/actions/workflows/ci.yml)
![Coverage](https://img.shields.io/badge/coverage-enabled-brightgreen)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![C%23](https://img.shields.io/badge/C%23-13-239120)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-planned-4169E1)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-planned-FF6600)

Este repositório documenta uma proposta de arquitetura de solução para uma plataforma de controle de fluxo de caixa, com foco em lançamentos financeiros e consolidação diária de saldo.

O objetivo é organizar a solução de forma progressiva, separando entendimento do problema, requisitos, decisões arquiteturais, desenho técnico e evolução da implementação.

## Contexto

Pequenos comerciantes precisam registrar lançamentos de crédito e débito ao longo do dia e consultar o saldo diário consolidado. A solução deve considerar a continuidade do serviço de lançamentos mesmo quando o processamento de consolidação estiver indisponível.

## Estrutura Inicial

```text
.
├── CashFlowArchitecture.slnx
├── README.md
├── docs
│   ├── adr
│   │   ├── 0001-separar-lancamentos-e-consolidacao.md
│   │   └── 0002-processar-consolidacao-de-forma-assincrona.md
│   ├── 01-contexto-e-objetivo.md
│   ├── 02-requisitos-iniciais.md
│   ├── 03-premissas-restricoes-e-decisoes.md
│   ├── 04-arquitetura-logica.md
│   ├── 05-contratos-api.md
│   ├── 06-modelo-de-dados.md
│   ├── 07-resiliencia-e-observabilidade.md
│   └── 08-estrategia-de-testes.md
├── src
│   └── CashFlowArchitecture.Api
│       ├── CashFlowArchitecture.Api.csproj
│       └── Program.cs
└── tests
    └── CashFlowArchitecture.Api.Tests
        ├── CashFlowArchitecture.Api.Tests.csproj
        └── FinancialEntryEndpointsTests.cs
```

## Documentação

- [Contexto e objetivo](docs/01-contexto-e-objetivo.md)
- [Requisitos iniciais](docs/02-requisitos-iniciais.md)
- [Premissas, restrições e decisões](docs/03-premissas-restricoes-e-decisoes.md)
- [Arquitetura lógica](docs/04-arquitetura-logica.md)
- [Contratos de API](docs/05-contratos-api.md)
- [Modelo de dados](docs/06-modelo-de-dados.md)
- [Resiliência e observabilidade](docs/07-resiliencia-e-observabilidade.md)
- [Estratégia de testes](docs/08-estrategia-de-testes.md)
- [ADR 0001 - Separar lançamentos e consolidação](docs/adr/0001-separar-lancamentos-e-consolidacao.md)
- [ADR 0002 - Processar consolidação de forma assíncrona](docs/adr/0002-processar-consolidacao-de-forma-assincrona.md)

## Idioma do Projeto

A documentação do projeto será mantida em português. Quando houver código, nomes técnicos de código, APIs, classes, métodos, serviços e variáveis serão escritos em inglês.

## Execução Local

### Pré-requisitos

Para executar o projeto localmente, instale:

- .NET SDK 10 ou superior.
- Visual Studio Code.
- Extensão C# Dev Kit ou extensão C# compatível com depuração .NET.

### Terminal

Para executar a API localmente:

```bash
dotnet run --project src/CashFlowArchitecture.Api/CashFlowArchitecture.Api.csproj
```

Para compilar a solução:

```bash
dotnet build CashFlowArchitecture.slnx
```

Para executar os testes automatizados:

```bash
dotnet test CashFlowArchitecture.slnx
```

Para executar os testes com coleta de cobertura:

```bash
dotnet test CashFlowArchitecture.slnx --collect:"XPlat Code Coverage" --results-directory TestResults
```

## Pipeline

O repositório possui um workflow do GitHub Actions em `.github/workflows/ci.yml`.

O pipeline executa:

1. Restore da solução.
2. Build em configuração `Release`.
3. Testes automatizados.
4. Coleta de cobertura com `XPlat Code Coverage`.
5. Upload dos resultados de teste como artefato do workflow.

O badge `Build and Test` no topo do README mostra o status do workflow na branch `main`. O badge de cobertura indica que a coleta de cobertura está habilitada no pipeline; o relatório gerado fica disponível nos artefatos da execução.

Endpoint inicial disponível:

```http
GET /health
```

Documentação navegável da API em ambiente de desenvolvimento:

```text
http://localhost:5099/swagger
```

Endpoints de lançamentos disponíveis nesta etapa:

```http
POST /entries
GET /entries?date=2026-09-01
POST /daily-balances/process-events
GET /daily-balances/2026-09-01
```

Nesta implementação, os lançamentos são persistidos localmente em arquivo JSON:

```text
src/CashFlowArchitecture.Api/data/financial-entries.json
```

Ao criar um lançamento, a API também registra um evento local `EntryCreated`:

```text
src/CashFlowArchitecture.Api/data/integration-events.json
```

Ao processar os eventos, a API atualiza a visão local de saldo consolidado:

```text
src/CashFlowArchitecture.Api/data/daily-balances.json
```

A pasta `data/` é ignorada pelo Git porque contém dados locais de execução. A persistência em banco de dados e a publicação em mensageria real podem ser adicionadas em uma etapa futura.

### Visual Studio Code

As configurações locais do VS Code ficam na pasta `.vscode/`. Essa pasta não é versionada porque pode conter preferências específicas de cada desenvolvedor.

Para recriar o ambiente de execução local no VS Code, primeiro descubra o caminho do .NET SDK na sua máquina:

```bash
which dotnet
```

Neste projeto, a configuração local foi criada usando:

```text
/usr/local/share/dotnet/dotnet
```

Se o seu caminho for diferente, substitua esse valor nos arquivos abaixo.

Arquivo `.vscode/settings.json`:

```json
{
  "dotnetAcquisitionExtension.sharedExistingDotnetPath": "/usr/local/share/dotnet/dotnet",
  "dotnetAcquisitionExtension.existingDotnetPath": [
    {
      "extensionId": "ms-dotnettools.csharp",
      "path": "/usr/local/share/dotnet/dotnet"
    },
    {
      "extensionId": "ms-dotnettools.csdevkit",
      "path": "/usr/local/share/dotnet/dotnet"
    }
  ],
  "terminal.integrated.env.osx": {
    "DOTNET_ROOT": "/usr/local/share/dotnet",
    "PATH": "/usr/local/share/dotnet:/usr/local/bin:/opt/homebrew/bin:${env:PATH}"
  }
}
```

Arquivo `.vscode/tasks.json`:

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "build-api",
      "command": "/usr/local/share/dotnet/dotnet",
      "type": "process",
      "args": [
        "build",
        "${workspaceFolder}/src/CashFlowArchitecture.Api/CashFlowArchitecture.Api.csproj"
      ],
      "problemMatcher": "$msCompile",
      "group": {
        "kind": "build",
        "isDefault": true
      }
    }
  ]
}
```

Arquivo `.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Run CashFlowArchitecture.Api",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build-api",
      "program": "${workspaceFolder}/src/CashFlowArchitecture.Api/bin/Debug/net10.0/CashFlowArchitecture.Api.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/CashFlowArchitecture.Api",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)",
        "uriFormat": "%s/swagger"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": "http://localhost:5099",
        "DOTNET_ROOT": "/usr/local/share/dotnet",
        "PATH": "/usr/local/share/dotnet:/usr/local/bin:/opt/homebrew/bin:${env:PATH}"
      },
      "sourceFileMap": {
        "/Views": "${workspaceFolder}/Views"
      }
    }
  ]
}
```

Depois disso:

1. Abra o repositório no VS Code.
2. Acesse a aba Run and Debug.
3. Selecione `Run CashFlowArchitecture.Api`.
4. Execute a aplicação.
5. O VS Code deve abrir `http://localhost:5099/swagger` automaticamente.

Se o navegador não abrir automaticamente, acesse manualmente:

```text
http://localhost:5099/swagger
```

Para validar a saúde da API:

```http
GET http://localhost:5099/health
```

Se o VS Code exibir erro como `dotnet: command not found` ao depurar, o problema é que o VS Code não encontrou o SDK do .NET no PATH usado pela extensão. Confirme o caminho com `which dotnet`, atualize `.vscode/settings.json`, `.vscode/tasks.json` e `.vscode/launch.json`, depois feche e abra o VS Code novamente.

No macOS, se o erro continuar mesmo com os arquivos `.vscode` configurados, abra o VS Code pelo terminal dentro da pasta do projeto:

```bash
code .
```

Isso faz o VS Code herdar o mesmo PATH do terminal. Outra alternativa é criar um atalho do comando `dotnet` em `/usr/local/bin`, apontando para a instalação real do SDK.

## Próximas Etapas

As próximas entregas devem evoluir o repositório em partes pequenas e commitáveis, por exemplo:

1. Refinar requisitos funcionais e não funcionais.
2. Evoluir persistência para PostgreSQL com EF Core quando necessário.
3. Evoluir o processamento local para worker assíncrono.
