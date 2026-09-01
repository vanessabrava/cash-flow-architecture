# Cash Flow Architecture

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

Endpoint inicial disponível:

```http
GET /health
```

Endpoints de lançamentos disponíveis nesta etapa:

```http
POST /entries
GET /entries?date=2026-09-01
```

Nesta primeira implementação, os lançamentos são mantidos em memória. A persistência em banco de dados será adicionada em uma etapa futura.

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
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
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
5. Acesse `http://localhost:5099/health`.

Se o VS Code exibir erro como `dotnet: command not found` ao depurar, o problema é que o VS Code não encontrou o SDK do .NET no PATH usado pela extensão. Confirme o caminho com `which dotnet`, atualize `.vscode/settings.json`, `.vscode/tasks.json` e `.vscode/launch.json`, depois feche e abra o VS Code novamente.

No macOS, se o erro continuar mesmo com os arquivos `.vscode` configurados, abra o VS Code pelo terminal dentro da pasta do projeto:

```bash
code .
```

Isso faz o VS Code herdar o mesmo PATH do terminal. Outra alternativa é criar um atalho do comando `dotnet` em `/usr/local/bin`, apontando para a instalação real do SDK.

## Próximas Etapas

As próximas entregas devem evoluir o repositório em partes pequenas e commitáveis, por exemplo:

1. Refinar requisitos funcionais e não funcionais.
2. Implementar persistência dos lançamentos financeiros.
3. Adicionar Swagger para documentação navegável da API.
