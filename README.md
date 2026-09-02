# Cash Flow Architecture

[![Build and Test](https://github.com/vanessabrava/cash-flow-architecture/actions/workflows/ci.yml/badge.svg)](https://github.com/vanessabrava/cash-flow-architecture/actions/workflows/ci.yml)
![Coverage](https://img.shields.io/badge/coverage-enabled-brightgreen)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![C%23](https://img.shields.io/badge/C%23-13-239120)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-4169E1)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-4-FF6600)

Este repositório documenta uma proposta de arquitetura de solução para uma plataforma de controle de fluxo de caixa, com foco em lançamentos financeiros e consolidação diária de saldo.

O objetivo é organizar a solução de forma progressiva, separando entendimento do problema, requisitos, decisões arquiteturais, desenho técnico e evolução da implementação.

## Contexto

Pequenos comerciantes precisam registrar lançamentos de crédito e débito ao longo do dia e consultar o saldo diário consolidado. A solução deve considerar a continuidade do serviço de lançamentos mesmo quando o processamento de consolidação estiver indisponível.

## Estrutura Inicial

```text
.
├── CashFlowArchitecture.slnx
├── compose.yaml
├── README.md
├── docs
│   ├── adr
│   │   ├── 0001-separar-lancamentos-e-consolidacao.md
│   │   ├── 0002-processar-consolidacao-de-forma-assincrona.md
│   │   └── 0003-usar-idempotency-key-na-criacao-de-lancamentos.md
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
- [ADR 0003 - Usar Idempotency-Key na criação de lançamentos](docs/adr/0003-usar-idempotency-key-na-criacao-de-lancamentos.md)

## Idioma do Projeto

A documentação do projeto será mantida em português. Quando houver código, nomes técnicos de código, APIs, classes, métodos, serviços e variáveis serão escritos em inglês.

## Execução Local

### Pré-requisitos

Para executar o projeto localmente, instale:

- .NET SDK 10 ou superior.
- Docker Desktop.
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

### Infraestrutura Local

O arquivo `compose.yaml` prepara a aplicação e os serviços planejados para a evolução da arquitetura:

- API .NET exposta localmente com Swagger.
- PostgreSQL 17 para persistência relacional.
- Adminer para consulta web do PostgreSQL local.
- RabbitMQ 4 com painel de gerenciamento para mensageria.
- Serviço temporário de migrations para criar ou atualizar o schema do PostgreSQL.
- Worker de consolidação para consumir eventos do RabbitMQ e atualizar o saldo diário.

#### Opção 1: executar infraestrutura no Docker e API pelo terminal ou F5

Use esta opção quando quiser depurar a API pelo Visual Studio Code.

Passo 1: criar o arquivo local de variáveis de ambiente.

```bash
cp .env.example .env
```

O arquivo `.env` guarda portas e credenciais locais de desenvolvimento. Ele é ignorado pelo Git.

Passo 2: subir PostgreSQL, Adminer e RabbitMQ.

```bash
docker compose up -d postgres adminer rabbitmq
```

Esse comando sobe apenas as dependências externas da API.

Passo 3: restaurar as ferramentas locais do .NET.

```bash
dotnet tool restore
```

Esse comando instala as ferramentas declaradas em `dotnet-tools.json`, incluindo `dotnet-ef`.

Passo 4: criar ou atualizar as tabelas no PostgreSQL local.

```bash
dotnet tool run dotnet-ef database update --project src/CashFlowArchitecture.Api/CashFlowArchitecture.Api.csproj --startup-project src/CashFlowArchitecture.Api/CashFlowArchitecture.Api.csproj
```

Esse comando aplica as migrations do EF Core no banco `cash_flow`.

Passo 5: executar a API pelo terminal ou pelo F5 do VS Code.

```bash
dotnet run --project src/CashFlowArchitecture.Api/CashFlowArchitecture.Api.csproj
```

Depois acesse:

```text
Swagger: http://localhost:5099/swagger
```

#### Opção 2: executar tudo pelo Docker Compose

Use esta opção quando quiser subir a aplicação inteira sem usar F5.

Passo 1: criar o arquivo local de variáveis de ambiente.

```bash
cp .env.example .env
```

O arquivo `.env` guarda portas e credenciais locais de desenvolvimento. Ele é ignorado pelo Git.

Passo 2: construir as imagens e subir todos os serviços.

```bash
docker compose up -d --build
```

Esse comando executa o fluxo completo:

1. Constrói a imagem da API.
2. Constrói a imagem do serviço de migrations.
3. Sobe o PostgreSQL.
4. Aguarda o PostgreSQL ficar saudável.
5. Executa o container temporário `cash-flow-migrations`.
6. Aplica as migrations do EF Core no banco `cash_flow`.
7. Sobe o RabbitMQ.
8. Sobe o Adminer.
9. Sobe a API em container.
10. Sobe o worker de consolidação em container separado.

O Docker Compose pode iniciar alguns serviços independentes em paralelo. As dependências importantes ficam controladas no compose:

1. O serviço `migrations` só executa depois que o PostgreSQL está saudável.
2. A API só sobe depois que o PostgreSQL está saudável, o RabbitMQ está saudável e o serviço `migrations` terminou com sucesso.
3. O worker só sobe depois que o PostgreSQL está saudável, o RabbitMQ está saudável e o serviço `migrations` terminou com sucesso.

O container `cash-flow-migrations` termina após aplicar as migrations. Isso é esperado. Ele não é uma aplicação contínua.

Passo 3: validar se os serviços subiram.

```bash
docker compose ps
```

O PostgreSQL e o RabbitMQ devem aparecer como saudáveis. A API e o worker de consolidação devem aparecer em execução.

Passo 4: acessar a aplicação e as ferramentas locais.

Serviços disponíveis:

```text
API Swagger: http://localhost:5099/swagger
PostgreSQL: localhost:5432
Adminer: http://localhost:8080
RabbitMQ: localhost:5672
RabbitMQ Management: http://localhost:15672
```

As credenciais padrão ficam em `.env.example`. O arquivo `.env` local é ignorado pelo Git.

Essas credenciais são apenas para desenvolvimento local. Elas são descartáveis e não devem ser reutilizadas em ambientes de homologação, produção ou qualquer ambiente compartilhado.

Credenciais locais padrão:

| Serviço | Usuário | Senha |
| --- | --- | --- |
| PostgreSQL | `cash_flow_user` | `cash_flow_password` |
| RabbitMQ Management | `cash_flow_user` | `cash_flow_password` |

Para acessar o PostgreSQL pelo Adminer, abra `http://localhost:8080` e preencha os campos exatamente assim:

| Campo no Adminer | Valor |
| --- | --- |
| System | `PostgreSQL` |
| Server | `postgres` |
| Username | `cash_flow_user` |
| Password | `cash_flow_password` |
| Database | `cash_flow` |

O campo `System` precisa estar como `PostgreSQL`. Se ele ficar como `MySQL / MariaDB`, o Adminer tentará conectar usando o protocolo errado e exibirá erro como `Connection refused`.

Connection string para a API executando fora do Docker, por exemplo via terminal ou F5 no VS Code:

```text
Host=localhost;Port=5432;Database=cash_flow;Username=cash_flow_user;Password=cash_flow_password
```

Connection string para um serviço executando dentro da mesma rede do Docker Compose:

```text
Host=postgres;Port=5432;Database=cash_flow;Username=cash_flow_user;Password=cash_flow_password
```

Para parar os serviços:

```bash
docker compose down
```

Para parar os serviços e apagar os volumes locais do PostgreSQL e RabbitMQ:

```bash
docker compose down -v
```

Use `docker compose down -v` apenas quando quiser limpar os dados locais e recriar o ambiente do zero.

## Pipeline

O repositório possui um workflow do GitHub Actions em `.github/workflows/ci.yml`.

O pipeline executa:

1. Restore da solução.
2. Build em configuração `Release`.
3. Testes automatizados.
4. Coleta de cobertura com `XPlat Code Coverage`.
5. Publicação de resumo visual dos testes no próprio GitHub Actions.
6. Upload dos resultados de teste como artefato do workflow.

O badge `Build and Test` no topo do README mostra o status do workflow na branch `main`. O badge de cobertura indica que a coleta de cobertura está habilitada no pipeline; o relatório gerado fica disponível nos artefatos da execução.

O resumo dos testes aparece na página da execução do workflow, sem precisar baixar o artefato apenas para ver quantos testes passaram ou falharam.

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

Nesta implementação, os lançamentos financeiros e os saldos consolidados são persistidos no PostgreSQL local por meio de EF Core.

```text
financial_entries
daily_balances
daily_balance_processed_events
idempotency_records
```

O endpoint `POST /entries` aceita o header opcional `Idempotency-Key`.

- Sem `Idempotency-Key`, cada chamada cria um novo lançamento.
- Com `Idempotency-Key`, repetir a mesma chave com o mesmo conteúdo retorna o lançamento já criado e não duplica o registro.
- Reutilizar a mesma chave com conteúdo diferente retorna `409 Conflict`.

Ao criar um lançamento, a API publica o evento `EntryCreated` no RabbitMQ.

Configuração local da mensageria:

```text
Exchange: cash-flow.events
Queue: cash-flow.entry-created
Routing key: entry.created
```

Nesta etapa, a API também mantém uma cópia local temporária do evento em arquivo JSON:

```text
src/CashFlowArchitecture.Api/data/integration-events.json
```

A pasta `data/` é ignorada pelo Git porque contém dados locais de execução.

Esse arquivo local existe apenas para manter o endpoint manual de consolidação funcionando durante a evolução do desafio.

Após cadastrar um lançamento, a fila `cash-flow.entry-created` deve aparecer no RabbitMQ Management.

Para validar no painel:

1. Abra `http://localhost:15672`.
2. Entre com usuário `cash_flow_user` e senha `cash_flow_password`.
3. Acesse a aba `Queues and Streams`.
4. Abra a fila `cash-flow.entry-created`.
5. Verifique se o contador de publicações aumentou após chamar `POST /entries`.

O worker `cash-flow-consolidation-worker` consome essa fila e atualiza o saldo consolidado no PostgreSQL.

Para validar a independência entre API e consolidação:

1. Pare apenas o worker:

```bash
docker stop cash-flow-consolidation-worker
```

2. Cadastre um lançamento pelo Swagger.
3. Consulte o saldo da data cadastrada.

Enquanto o worker estiver parado, a API continua registrando lançamentos, mas o saldo pode retornar `PENDING`.

4. Ligue novamente o worker:

```bash
docker compose up -d consolidation-worker
```

5. Consulte o saldo novamente.

Depois que o worker consumir a mensagem pendente, o saldo deve retornar `CONSOLIDATED`.

O endpoint abaixo permanece disponível como apoio temporário para processamento manual durante o desenvolvimento:

```http
POST /daily-balances/process-events
```

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
        "ConnectionStrings__Postgres": "Host=localhost;Port=5432;Database=cash_flow;Username=cash_flow_user;Password=cash_flow_password",
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
3. Separar o worker em um projeto .NET próprio dentro da solution.
