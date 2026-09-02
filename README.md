# Cash Flow Architecture

[![Build and Test](https://github.com/vanessabrava/cash-flow-architecture/actions/workflows/ci.yml/badge.svg)](https://github.com/vanessabrava/cash-flow-architecture/actions/workflows/ci.yml)
![Coverage](https://img.shields.io/badge/coverage-enabled-brightgreen)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![C%23](https://img.shields.io/badge/C%23-13-239120)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-4169E1)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-4-FF6600)
![Redis](https://img.shields.io/badge/Redis-8-DC382D)

Este repositório documenta uma proposta de arquitetura de solução para uma plataforma de controle de fluxo de caixa, com foco em lançamentos financeiros e consolidação diária de saldo.

O objetivo é organizar a solução de forma progressiva, separando entendimento do problema, requisitos, decisões arquiteturais, desenho técnico e evolução da implementação.

## Contexto

Pequenos comerciantes precisam registrar lançamentos de crédito e débito ao longo do dia e consultar o saldo diário consolidado. A solução deve considerar a continuidade do serviço de lançamentos mesmo quando o processamento de consolidação estiver indisponível.

## Estrutura Atual

```text
.
├── CashFlowArchitecture.slnx
├── compose.yaml
├── README.md
├── docs
│   ├── adr
│   │   ├── 0001-separar-lancamentos-e-consolidacao.md
│   │   ├── 0002-processar-consolidacao-de-forma-assincrona.md
│   │   ├── 0003-usar-idempotency-key-na-criacao-de-lancamentos.md
│   │   ├── 0004-modularizar-api-worker-core-e-infrastructure.md
│   │   ├── 0005-usar-api-key-local-para-protecao-inicial.md
│   │   ├── 0006-usar-redis-para-cache-de-saldo-diario.md
│   │   ├── 0007-usar-outbox-para-publicacao-confiavel-de-eventos.md
│   │   ├── 0008-controlar-retentativas-da-outbox.md
│   │   ├── 0009-separar-liveness-e-readiness.md
│   │   ├── 0010-registrar-logs-http-estruturados.md
│   │   └── 0011-usar-json-console-logs.md
│   ├── 01-contexto-e-objetivo.md
│   ├── 02-requisitos-iniciais.md
│   ├── 03-premissas-restricoes-e-decisoes.md
│   ├── 04-arquitetura-logica.md
│   ├── 05-contratos-api.md
│   ├── 06-modelo-de-dados.md
│   ├── 07-resiliencia-e-observabilidade.md
│   └── 08-estrategia-de-testes.md
├── src
│   ├── CashFlowArchitecture.Api
│   │   ├── CashFlowArchitecture.Api.csproj
│   │   └── Program.cs
│   ├── CashFlowArchitecture.Consolidation.Api
│   │   ├── CashFlowArchitecture.Consolidation.Api.csproj
│   │   └── Program.cs
│   ├── CashFlowArchitecture.Core
│   │   └── CashFlowArchitecture.Core.csproj
│   ├── CashFlowArchitecture.Infrastructure
│   │   └── CashFlowArchitecture.Infrastructure.csproj
│   └── CashFlowArchitecture.Worker
│       ├── CashFlowArchitecture.Worker.csproj
│       ├── Dockerfile
│       └── Program.cs
└── tests
    ├── CashFlowArchitecture.Api.Tests
    │   ├── CashFlowArchitecture.Api.Tests.csproj
    │   └── FinancialEntryEndpointsTests.cs
    └── CashFlowArchitecture.Consolidation.Api.Tests
        ├── CashFlowArchitecture.Consolidation.Api.Tests.csproj
        └── DailyBalanceEndpointsTests.cs
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
- [ADR 0004 - Modularizar API, Worker, Core e Infrastructure](docs/adr/0004-modularizar-api-worker-core-e-infrastructure.md)
- [ADR 0005 - Usar API Key local para proteção inicial](docs/adr/0005-usar-api-key-local-para-protecao-inicial.md)
- [ADR 0006 - Usar Redis para cache de saldo diário](docs/adr/0006-usar-redis-para-cache-de-saldo-diario.md)
- [ADR 0007 - Usar Outbox para publicação confiável de eventos](docs/adr/0007-usar-outbox-para-publicacao-confiavel-de-eventos.md)
- [ADR 0008 - Controlar retentativas da Outbox](docs/adr/0008-controlar-retentativas-da-outbox.md)
- [ADR 0009 - Separar liveness e readiness](docs/adr/0009-separar-liveness-e-readiness.md)
- [ADR 0010 - Registrar logs HTTP estruturados](docs/adr/0010-registrar-logs-http-estruturados.md)
- [ADR 0011 - Usar JSON console logs](docs/adr/0011-usar-json-console-logs.md)

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

Para executar a API de lançamentos localmente:

```bash
dotnet run --project src/CashFlowArchitecture.Api/CashFlowArchitecture.Api.csproj
```

Para executar a API de consolidação localmente:

```bash
dotnet run --project src/CashFlowArchitecture.Consolidation.Api/CashFlowArchitecture.Consolidation.Api.csproj
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

O arquivo `compose.yaml` prepara a aplicação e os serviços necessários para a execução local da arquitetura:

- API de lançamentos exposta localmente com Swagger.
- API de consolidação exposta localmente com Swagger.
- PostgreSQL 17 para persistência relacional.
- Adminer para consulta web do PostgreSQL local.
- RabbitMQ 4 com painel de gerenciamento para mensageria.
- Redis 8 para cache de consultas de saldo diário consolidado.
- Redis Commander para consulta web das chaves do Redis local.
- Outbox no PostgreSQL para publicação confiável de eventos.
- Retentativas controladas da Outbox para falhas temporárias de publicação.
- Serviço temporário de migrations para criar ou atualizar o schema do PostgreSQL.
- Worker de consolidação para consumir eventos do RabbitMQ e atualizar o saldo diário.

#### Opção 1: executar infraestrutura no Docker e serviços pelo terminal ou F5

Use esta opção quando quiser depurar as APIs pelo Visual Studio Code.

Passo 1: criar o arquivo local de variáveis de ambiente.

```bash
cp .env.example .env
```

O arquivo `.env` guarda portas e credenciais locais de desenvolvimento. Ele é ignorado pelo Git.

Passo 2: subir PostgreSQL, Adminer, RabbitMQ e Redis.

```bash
docker compose up -d postgres adminer rabbitmq redis
```

Esse comando sobe apenas as dependências externas das APIs e do worker.

Passo 3: restaurar as ferramentas locais do .NET.

```bash
dotnet tool restore
```

Esse comando instala as ferramentas declaradas em `dotnet-tools.json`, incluindo `dotnet-ef`.

Passo 4: criar ou atualizar as tabelas no PostgreSQL local.

```bash
dotnet tool run dotnet-ef database update --project src/CashFlowArchitecture.Infrastructure/CashFlowArchitecture.Infrastructure.csproj --startup-project src/CashFlowArchitecture.Infrastructure/CashFlowArchitecture.Infrastructure.csproj
```

Esse comando aplica as migrations do EF Core no banco `cash_flow`.

Passo 5: executar a API de lançamentos pelo terminal ou pelo F5 do VS Code.

```bash
dotnet run --project src/CashFlowArchitecture.Api/CashFlowArchitecture.Api.csproj
```

Depois acesse:

```text
Swagger de lançamentos: http://localhost:5099/swagger
```

Passo 6: executar a API de consolidação em outro terminal ou pelo F5 do VS Code.

```bash
dotnet run --project src/CashFlowArchitecture.Consolidation.Api/CashFlowArchitecture.Consolidation.Api.csproj
```

Depois acesse:

```text
Swagger de consolidação: http://localhost:5100/swagger
```

Passo 7: executar o worker de consolidação em outro terminal, se quiser validar a consolidação automática fora do Docker Compose completo.

```bash
dotnet run --project src/CashFlowArchitecture.Worker/CashFlowArchitecture.Worker.csproj
```

Sem o worker em execução, a API de lançamentos continua cadastrando lançamentos e publicando eventos no RabbitMQ, mas a API de consolidação pode retornar saldo `PENDING` até o processamento acontecer.

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

1. Constrói a imagem da API de lançamentos.
2. Constrói a imagem da API de consolidação.
3. Constrói a imagem do worker de consolidação.
4. Constrói a imagem do serviço de migrations.
5. Sobe o PostgreSQL.
6. Aguarda o PostgreSQL ficar saudável.
7. Sobe o Redis.
8. Executa o container temporário `cash-flow-migrations`.
9. Aplica as migrations do EF Core no banco `cash_flow`.
10. Sobe o RabbitMQ.
11. Sobe o Adminer.
12. Sobe a API de lançamentos em container.
13. Sobe a API de consolidação em container separado.
14. Sobe o worker de consolidação em container separado.

O Docker Compose pode iniciar alguns serviços independentes em paralelo. As dependências importantes ficam controladas no compose:

1. O serviço `migrations` só executa depois que o PostgreSQL está saudável.
2. A API de lançamentos só sobe depois que o PostgreSQL está saudável e o serviço `migrations` terminou com sucesso.
3. A API de consolidação só sobe depois que o PostgreSQL e o Redis estão saudáveis e o serviço `migrations` terminou com sucesso.
4. O worker só sobe depois que o PostgreSQL, o RabbitMQ e o Redis estão saudáveis e o serviço `migrations` terminou com sucesso.

A API de lançamentos não depende do RabbitMQ nem da API de consolidação para iniciar. Se o RabbitMQ estiver temporariamente indisponível, os eventos ficam pendentes na tabela `outbox_messages` e são publicados quando o canal de mensageria voltar.

O container `cash-flow-migrations` termina após aplicar as migrations. Isso é esperado. Ele não é uma aplicação contínua.

Passo 3: validar se os serviços subiram.

```bash
docker compose ps
```

O PostgreSQL, o RabbitMQ e o Redis devem aparecer como saudáveis. As duas APIs e o worker de consolidação devem aparecer em execução.

Passo 4: acessar a aplicação e as ferramentas locais.

Serviços disponíveis:

```text
API de lançamentos Swagger: http://localhost:5099/swagger
API de consolidação Swagger: http://localhost:5100/swagger
PostgreSQL: localhost:5432
Adminer: http://localhost:8080
Redis: localhost:6379
Redis Commander: http://localhost:8081
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
| Redis | Não se aplica | `cash_flow_redis_password` |
| Redis Commander | `cash_flow_user` | `cash_flow_password` |

Configuração local da Outbox:

| Variável | Valor padrão | Uso |
| --- | --- | --- |
| `OUTBOX_BATCH_SIZE` | `20` | Quantidade máxima de mensagens publicadas por ciclo. |
| `OUTBOX_MAX_RETRY_COUNT` | `5` | Limite de tentativas antes de marcar falha definitiva. |
| `OUTBOX_RETRY_DELAY_SECONDS` | `30` | Tempo de espera entre tentativas após falha. |
| `OUTBOX_PUBLISH_INTERVAL_SECONDS` | `5` | Intervalo entre ciclos de busca por mensagens publicáveis. |

Chave local padrão da API:

```text
X-Api-Key: cash_flow_local_api_key
```

A chave acima é apenas para desenvolvimento local. Em ambientes reais, ela deve ser substituída por uma estratégia de autenticação e autorização adequada, como OAuth2, OpenID Connect, JWT, API Gateway ou identidade serviço-a-serviço.

Para acessar o PostgreSQL pelo Adminer, abra `http://localhost:8080` e preencha os campos exatamente assim:

| Campo no Adminer | Valor |
| --- | --- |
| System | `PostgreSQL` |
| Server | `postgres` |
| Username | `cash_flow_user` |
| Password | `cash_flow_password` |
| Database | `cash_flow` |

O campo `System` precisa estar como `PostgreSQL`. Se ele ficar como `MySQL / MariaDB`, o Adminer tentará conectar usando o protocolo errado e exibirá erro como `Connection refused`.

Connection string para as APIs executando fora do Docker, por exemplo via terminal ou F5 no VS Code:

```text
Host=localhost;Port=5432;Database=cash_flow;Username=cash_flow_user;Password=cash_flow_password;GSS Encryption Mode=Disable
```

Connection string do Redis para a API de consolidação executando fora do Docker:

```text
localhost:6379,password=cash_flow_redis_password,abortConnect=false
```

Connection string para um serviço executando dentro da mesma rede do Docker Compose:

```text
Host=postgres;Port=5432;Database=cash_flow;Username=cash_flow_user;Password=cash_flow_password;GSS Encryption Mode=Disable
```

A opção `GSS Encryption Mode=Disable` evita que o driver PostgreSQL tente usar GSS/Kerberos no ambiente local em container. Isso remove mensagens nativas desnecessárias sobre `libgssapi_krb5.so.2` nos logs.

Connection string do Redis para um serviço executando dentro da mesma rede do Docker Compose:

```text
redis:6379,password=cash_flow_redis_password,abortConnect=false
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

Endpoints operacionais disponíveis nas duas APIs:

```http
GET /health
GET /health/live
GET /health/ready
```

Os endpoints de saúde são públicos para facilitar verificação de disponibilidade local.

O endpoint `GET /health/live` indica se o processo da API consultada está vivo.

O endpoint `GET /health/ready` indica se a API consultada está pronta para operar.

Na API de lançamentos:

- PostgreSQL é dependência crítica, porque a API precisa dele para gravar lançamentos.
- RabbitMQ é dependência não crítica, porque a Outbox permite publicar eventos depois.

Na API de consolidação:

- PostgreSQL é dependência crítica, porque a API precisa dele para consultar a fonte da verdade dos saldos.
- Redis é dependência não crítica, porque a API de consolidação consegue consultar PostgreSQL se o cache falhar.

Se uma dependência crítica falhar, o readiness retorna `503 Service Unavailable`. Se apenas dependências não críticas falharem, retorna `200 OK` com status `Degraded`.

As APIs registram logs HTTP estruturados com método, rota, status code, duração e `correlationId`. APIs e worker escrevem logs no console em formato JSON, com timestamp em UTC. Os logs não devem registrar payloads, senhas ou API Keys.

Documentação navegável das APIs em ambiente de desenvolvimento:

```text
API de lançamentos: http://localhost:5099/swagger
API de consolidação: http://localhost:5100/swagger
```

Endpoints de lançamentos disponíveis na API de lançamentos:

```http
POST /entries
GET /entries?date=2026-09-01
```

Endpoints de saldo disponíveis na API de consolidação:

```http
POST /daily-balances/process-events
GET /daily-balances/2026-09-01
```

Os endpoints de negócio exigem o header:

```http
X-Api-Key: cash_flow_local_api_key
```

No Swagger, clique em `Authorize`, informe `cash_flow_local_api_key` e confirme. Depois disso, o Swagger envia o header `X-Api-Key` automaticamente nas chamadas protegidas.

Nesta implementação, os lançamentos financeiros e os saldos consolidados são persistidos no PostgreSQL local por meio de EF Core.

```text
financial_entries
daily_balances
daily_balance_processed_events
idempotency_records
outbox_messages
```

O endpoint `POST /entries` aceita o header opcional `Idempotency-Key`.

- Sem `Idempotency-Key`, cada chamada cria um novo lançamento.
- Com `Idempotency-Key`, repetir a mesma chave com o mesmo conteúdo retorna o lançamento já criado e não duplica o registro.
- Reutilizar a mesma chave com conteúdo diferente retorna `409 Conflict`.

Ao criar um lançamento, a API de lançamentos grava o evento `EntryCreated` na tabela `outbox_messages` no PostgreSQL. Uma rotina em segundo plano lê mensagens pendentes da Outbox e publica no RabbitMQ.

Esse desenho reduz o risco de o lançamento ser salvo sem que o evento de consolidação seja publicado.

Configuração local da mensageria:

```text
Exchange: cash-flow.events
Queue: cash-flow.entry-created
Routing key: entry.created
```

Quando executada no modo de armazenamento em arquivo, a API de lançamentos mantém uma cópia local temporária do evento em arquivo JSON:

```text
src/CashFlowArchitecture.Api/data/integration-events.json
```

A pasta `data/` é ignorada pelo Git porque contém dados locais de execução.

Esse arquivo local existe apenas como apoio de desenvolvimento para cenários sem PostgreSQL/RabbitMQ. Para processamento manual em serviços separados, a API de consolidação precisa apontar para o mesmo caminho configurado em `Storage:IntegrationEventsPath`. Na execução principal com Docker Compose, o fluxo usa PostgreSQL, Outbox, RabbitMQ e worker.

Após cadastrar um lançamento, a fila `cash-flow.entry-created` deve aparecer no RabbitMQ Management.

Também é possível validar a Outbox pelo Adminer consultando a tabela:

```text
outbox_messages
```

Mensagens ainda não publicadas ficam com `processed_at` vazio. Após publicação com sucesso no RabbitMQ, `processed_at` é preenchido.

Se a publicação falhar, a Outbox registra:

| Campo | Significado |
| --- | --- |
| `retry_count` | Quantidade de tentativas já realizadas. |
| `next_attempt_at` | Próximo horário em que a mensagem pode ser publicada novamente. |
| `last_error` | Último erro registrado. |
| `failed_at` | Horário em que a mensagem atingiu o limite de tentativas e deixou de ser republicada automaticamente. |

Para validar no painel:

1. Abra `http://localhost:15672`.
2. Entre com usuário `cash_flow_user` e senha `cash_flow_password`.
3. Acesse a aba `Queues and Streams`.
4. Abra a fila `cash-flow.entry-created`.
5. Verifique se o contador de publicações aumentou após chamar `POST /entries`.

O worker `cash-flow-consolidation-worker` consome essa fila e atualiza o saldo consolidado no PostgreSQL.

O Redis é usado como cache de leitura para o endpoint da API de consolidação:

```http
GET /daily-balances/2026-09-01
```

O PostgreSQL continua sendo a fonte da verdade. O fluxo esperado é:

1. O worker consolida o saldo no PostgreSQL.
2. Após consolidar, o worker atualiza o Redis com o saldo atualizado.
3. A API de consolidação consulta Redis primeiro ao receber `GET /daily-balances/{date}`.
4. Se o saldo não estiver no Redis, a API de consolidação consulta o PostgreSQL.
5. Se o Redis estiver indisponível, a API de consolidação continua consultando o PostgreSQL.

O TTL inicial do cache é de 15 minutos. Ele existe como proteção operacional para evitar saldo antigo preso indefinidamente se houver falha entre PostgreSQL e Redis. A atualização principal do cache acontece na consolidação, não pela expiração.

Para consultar o Redis pelo navegador, abra:

```text
http://localhost:8081
```

Use:

| Campo | Valor |
| --- | --- |
| Username | `cash_flow_user` |
| Password | `cash_flow_password` |

Depois de acessar, procure chaves no formato:

```text
cash-flow:daily-balance:YYYY-MM-DD
```

Exemplo:

```text
cash-flow:daily-balance:2026-09-02
```

O Redis Commander é apenas uma ferramenta de desenvolvimento local. Ele não deve ser exposto em homologação, produção ou qualquer ambiente compartilhado sem controles adequados de rede, autenticação e autorização.

Para validar a independência entre lançamentos e consolidação:

1. Pare apenas o worker:

```bash
docker stop cash-flow-consolidation-worker
```

2. Cadastre um lançamento pelo Swagger da API de lançamentos em `http://localhost:5099/swagger`.
3. Consulte o saldo da data cadastrada pelo Swagger da API de consolidação em `http://localhost:5100/swagger`.

Enquanto o worker estiver parado, a API de lançamentos continua registrando lançamentos, mas o saldo pode retornar `PENDING`.

4. Ligue novamente o worker:

```bash
docker compose up -d consolidation-worker
```

5. Consulte o saldo novamente.

Depois que o worker consumir a mensagem pendente, o saldo deve retornar `CONSOLIDATED`.

Também é possível parar apenas a API de consolidação:

```bash
docker stop cash-flow-consolidation-api
```

Mesmo com a API de consolidação parada, a API de lançamentos em `http://localhost:5099/swagger` continua aceitando `POST /entries`. Isso demonstra que a escrita não depende da consulta de saldo para continuar operando.

Para religar a API de consolidação:

```bash
docker compose up -d consolidation-api
```

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
    },
    {
      "label": "build-consolidation-api",
      "command": "/usr/local/share/dotnet/dotnet",
      "type": "process",
      "args": [
        "build",
        "${workspaceFolder}/src/CashFlowArchitecture.Consolidation.Api/CashFlowArchitecture.Consolidation.Api.csproj"
      ],
      "problemMatcher": "$msCompile"
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
        "Authentication__ApiKey": "cash_flow_local_api_key",
        "ConnectionStrings__Postgres": "Host=localhost;Port=5432;Database=cash_flow;Username=cash_flow_user;Password=cash_flow_password;GSS Encryption Mode=Disable",
        "Outbox__BatchSize": "20",
        "Outbox__MaxRetryCount": "5",
        "Outbox__RetryDelaySeconds": "30",
        "Outbox__PublishIntervalSeconds": "5",
        "DOTNET_ROOT": "/usr/local/share/dotnet",
        "PATH": "/usr/local/share/dotnet:/usr/local/bin:/opt/homebrew/bin:${env:PATH}"
      },
      "sourceFileMap": {
        "/Views": "${workspaceFolder}/Views"
      }
    },
    {
      "name": "Run CashFlowArchitecture.Consolidation.Api",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build-consolidation-api",
      "program": "${workspaceFolder}/src/CashFlowArchitecture.Consolidation.Api/bin/Debug/net10.0/CashFlowArchitecture.Consolidation.Api.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/CashFlowArchitecture.Consolidation.Api",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)",
        "uriFormat": "%s/swagger"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": "http://localhost:5100",
        "Authentication__ApiKey": "cash_flow_local_api_key",
        "ConnectionStrings__Postgres": "Host=localhost;Port=5432;Database=cash_flow;Username=cash_flow_user;Password=cash_flow_password;GSS Encryption Mode=Disable",
        "ConnectionStrings__Redis": "localhost:6379,password=cash_flow_redis_password,abortConnect=false",
        "Redis__InstanceName": "cash-flow:",
        "Redis__DailyBalanceTtlMinutes": "15",
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
3. Selecione `Run CashFlowArchitecture.Api` para subir a API de lançamentos.
4. Selecione `Run CashFlowArchitecture.Consolidation.Api` para subir a API de consolidação.
5. O VS Code deve abrir o Swagger da configuração escolhida automaticamente.

Se o navegador não abrir automaticamente, acesse manualmente:

```text
API de lançamentos: http://localhost:5099/swagger
API de consolidação: http://localhost:5100/swagger
```

Para validar a saúde da API de lançamentos:

```http
GET http://localhost:5099/health
GET http://localhost:5099/health/live
GET http://localhost:5099/health/ready
```

Para validar a saúde da API de consolidação:

```http
GET http://localhost:5100/health
GET http://localhost:5100/health/live
GET http://localhost:5100/health/ready
```

Para validar um endpoint protegido, informe o header `X-Api-Key`:

```bash
curl -H "X-Api-Key: cash_flow_local_api_key" "http://localhost:5099/entries?date=2026-09-01"
```

Se o VS Code exibir erro como `dotnet: command not found` ao depurar, o problema é que o VS Code não encontrou o SDK do .NET no PATH usado pela extensão. Confirme o caminho com `which dotnet`, atualize `.vscode/settings.json`, `.vscode/tasks.json` e `.vscode/launch.json`, depois feche e abra o VS Code novamente.

No macOS, se o erro continuar mesmo com os arquivos `.vscode` configurados, abra o VS Code pelo terminal dentro da pasta do projeto:

```bash
code .
```

Isso faz o VS Code herdar o mesmo PATH do terminal. Outra alternativa é criar um atalho do comando `dotnet` em `/usr/local/bin`, apontando para a instalação real do SDK.

## Evoluções Para Produção

Os itens abaixo não são pendências para executar o desafio localmente. Eles indicam como a solução poderia evoluir em um ambiente produtivo:

1. Evoluir a autenticação local por API Key para OAuth2, OpenID Connect, JWT, API Gateway ou identidade serviço-a-serviço.
2. Implementar autorização por escopo, perfil ou recurso.
3. Evoluir a Outbox para backoff exponencial, fila de erro dedicada e reprocessamento administrativo.
4. Evoluir retry e observabilidade da atualização de cache após consolidação.
5. Criar testes de integração com PostgreSQL, RabbitMQ e Redis em containers.
6. Detalhar observabilidade com métricas, tracing e dashboards.
