# Contratos de API

Este documento descreve os contratos HTTP definidos para as capacidades principais da solução. Os contratos são separados entre API de Lançamentos e API de Saldo Consolidado.

## Convenções

- O formato de troca de dados será JSON.
- Datas serão representadas no formato ISO 8601.
- Valores monetários serão representados como número decimal.
- Identificadores públicos serão representados como texto em formato UID/GUID.
- Nomes de campos de API serão escritos em inglês.
- O ID interno do banco de dados não será exposto nas APIs.
- O ID interno será usado apenas para índices, chaves internas e relacionamentos na persistência.
- Endpoints de negócio devem exigir autenticação.
- A autenticação local desta etapa usa o header `X-Api-Key`.
- Requisições devem aceitar o header `X-Correlation-Id` para rastreabilidade ponta a ponta.
- Quando o consumidor não enviar `X-Correlation-Id`, a API deve gerar um novo `correlationId`.
- Respostas devem retornar o `correlationId` para facilitar diagnóstico e suporte.
- A criação de lançamentos deve aceitar o header `Idempotency-Key` para evitar duplicidade causada por retry do consumidor.

## Autenticação

Nesta etapa, os endpoints de negócio exigem uma API Key enviada no header `X-Api-Key`.

Exemplo:

```http
X-Api-Key: cash_flow_local_api_key
```

Essa chave é uma proteção inicial para execução local e demonstração do desafio. Ela não representa a estratégia definitiva para produção.

Em uma evolução real da solução, a autenticação e autorização devem avançar para OAuth2, OpenID Connect, JWT, API Gateway ou identidade serviço-a-serviço, com escopos e permissões por recurso.

### Endpoints Públicos

| Endpoint | Motivo |
| --- | --- |
| `GET /health` | Permite verificar disponibilidade básica da aplicação. |
| `GET /health/live` | Permite verificar se o processo da API está vivo. |
| `GET /health/ready` | Permite verificar se a API está pronta para operar com dependências críticas. |
| `/swagger` | Permite testar a API em ambiente de desenvolvimento. |

### Endpoints Protegidos

| Serviço | Endpoint | Autenticação |
| --- | --- | --- |
| API de Lançamentos | `POST /entries` | Requer `X-Api-Key`. |
| API de Lançamentos | `GET /entries?date=YYYY-MM-DD` | Requer `X-Api-Key`. |
| API de Saldo Consolidado | `POST /daily-balances/process-events` | Requer `X-Api-Key`. |
| API de Saldo Consolidado | `GET /daily-balances/{date}` | Requer `X-Api-Key`. |

### Erro de Autenticação

Quando o header não for informado ou a chave for inválida, a API deve retornar:

```http
401 Unauthorized
```

```json
{
  "correlationId": "4a11b94c-45b7-4a48-9cb4-917ecf2c7f31",
  "code": "AUTHENTICATION_REQUIRED",
  "message": "Informe uma API Key válida para acessar este recurso.",
  "details": []
}
```

## Rastreabilidade

O `correlationId` identifica uma jornada técnica entre componentes. Ele não identifica uma entidade de negócio, como lançamento ou saldo, mas ajuda a rastrear uma requisição desde a entrada na API até a publicação do evento e o processamento da consolidação.

### Header de Requisição

```http
X-Correlation-Id: 4a11b94c-45b7-4a48-9cb4-917ecf2c7f31
```

### Header de Resposta

```http
X-Correlation-Id: 4a11b94c-45b7-4a48-9cb4-917ecf2c7f31
```

## API de Lançamentos

Responsável por registrar e consultar lançamentos financeiros.

Na execução local, essa API é exposta em:

```text
http://localhost:5099/swagger
```

### Criar Lançamento

```http
POST /entries
```

#### Headers

```http
X-Correlation-Id: 4a11b94c-45b7-4a48-9cb4-917ecf2c7f31
Idempotency-Key: 8d7f7d9c-6b3b-4a0c-8f7a-123456789abc
X-Api-Key: cash_flow_local_api_key
```

| Header | Obrigatório | Descrição |
| --- | --- | --- |
| X-Api-Key | Sim | Chave de autenticação local para acessar endpoints de negócio. |
| X-Correlation-Id | Não | Identificador de rastreabilidade técnica da requisição. |
| Idempotency-Key | Não | Identificador da tentativa lógica de criação. Quando repetido, deve retornar o lançamento já criado sem duplicar o registro. |

Quando `Idempotency-Key` não for informado, cada requisição `POST /entries` deve ser tratada como uma nova criação.

Isso significa que duas chamadas iguais sem `Idempotency-Key` podem criar dois lançamentos diferentes. Esse comportamento é intencional, porque duas vendas com mesmo tipo, valor, descrição e data podem ser legítimas.

Quando `Idempotency-Key` for informado:

- a primeira requisição válida retorna `201 Created`;
- a repetição da mesma chave com o mesmo conteúdo retorna `200 OK` com o mesmo lançamento já criado;
- a repetição da mesma chave com conteúdo diferente retorna `409 Conflict`;
- a repetição da mesma chave não deve criar novo lançamento nem novo evento de integração.

No Swagger da API de Lançamentos, esse header deve aparecer como parâmetro opcional da operação `POST /entries`.

#### Requisição

```json
{
  "type": "CREDIT",
  "amount": 150.75,
  "description": "Venda no cartão",
  "entryDate": "2026-09-01"
}
```

#### Campos

| Campo | Tipo | Obrigatório | Descrição |
| --- | --- | --- | --- |
| type | string | Sim | Tipo do lançamento. Valores esperados: `CREDIT` ou `DEBIT`. |
| amount | decimal | Sim | Valor do lançamento. Deve ser maior que zero. |
| description | string | Sim | Descrição curta do lançamento. |
| entryDate | date | Sim | Data de referência do lançamento. |

#### Resposta de Sucesso

```http
201 Created
```

```json
{
  "correlationId": "4a11b94c-45b7-4a48-9cb4-917ecf2c7f31",
  "uid": "0f0b8b8d-5022-4b62-9e38-7b6f6a87f121",
  "type": "CREDIT",
  "amount": 150.75,
  "description": "Venda no cartão",
  "entryDate": "2026-09-01",
  "createdAt": "2026-09-01T10:15:30Z"
}
```

#### Resposta em Replay Idempotente

```http
200 OK
```

Retorna o mesmo contrato da criação, preservando o `uid` do lançamento criado na primeira chamada.

#### Resposta de Conflito de Idempotência

```http
409 Conflict
```

```json
{
  "correlationId": "4a11b94c-45b7-4a48-9cb4-917ecf2c7f31",
  "code": "IDEMPOTENCY_KEY_CONFLICT",
  "message": "A Idempotency-Key informada já foi usada com outro conteúdo.",
  "details": [
    {
      "field": "Idempotency-Key",
      "message": "Use uma nova chave para uma nova tentativa lógica de criação."
    }
  ]
}
```

### Consultar Lançamentos por Data

```http
GET /entries?date=2026-09-01
```

#### Resposta de Sucesso

```http
200 OK
```

```json
{
  "correlationId": "4a11b94c-45b7-4a48-9cb4-917ecf2c7f31",
  "date": "2026-09-01",
  "items": [
    {
      "uid": "0f0b8b8d-5022-4b62-9e38-7b6f6a87f121",
      "type": "CREDIT",
      "amount": 150.75,
      "description": "Venda no cartão",
      "entryDate": "2026-09-01",
      "createdAt": "2026-09-01T10:15:30Z"
    },
    {
      "uid": "1b20c0d9-4a10-4ec9-8903-83a87d4c5f12",
      "type": "DEBIT",
      "amount": 40.00,
      "description": "Pagamento de fornecedor",
      "entryDate": "2026-09-01",
      "createdAt": "2026-09-01T11:20:00Z"
    }
  ]
}
```

## API de Saldo Consolidado

Responsável por consultar o saldo diário consolidado.

Na execução local, essa API é exposta em:

```text
http://localhost:5100/swagger
```

### Processar Eventos de Consolidação

```http
POST /daily-balances/process-events
```

#### Resposta de Sucesso

```http
200 OK
```

```json
{
  "correlationId": "4a11b94c-45b7-4a48-9cb4-917ecf2c7f31",
  "processedEvents": 2,
  "skippedEvents": 0,
  "updatedBalances": 1
}
```

Esse endpoint representa um processamento local simplificado dos eventos `EntryCreated`. Ele permanece disponível como apoio temporário para desenvolvimento e diagnóstico.

Na implementação atual, a consolidação assíncrona principal é executada pelo worker `cash-flow-consolidation-worker`, que consome eventos do RabbitMQ. Quando a aplicação roda no modo de armazenamento em arquivo, a API de lançamentos pode gravar eventos em JSON e a API de consolidação pode processar esse mesmo arquivo quando ambas apontarem para o mesmo `Storage:IntegrationEventsPath`.

### Consultar Saldo Diário

```http
GET /daily-balances/2026-09-01
```

#### Resposta de Sucesso

```http
200 OK
```

```json
{
  "correlationId": "4a11b94c-45b7-4a48-9cb4-917ecf2c7f31",
  "date": "2026-09-01",
  "totalCredits": 150.75,
  "totalDebits": 40.00,
  "balance": 110.75,
  "status": "CONSOLIDATED",
  "updatedAt": "2026-09-01T11:21:10Z"
}
```

#### Saldo Ainda Não Consolidado

Quando a consolidação ainda não tiver sido processada para a data solicitada, a API pode retornar:

```http
202 Accepted
```

```json
{
  "correlationId": "4a11b94c-45b7-4a48-9cb4-917ecf2c7f31",
  "date": "2026-09-01",
  "status": "PENDING",
  "message": "Saldo diário ainda não consolidado."
}
```

## Respostas de Erro

### Erro de Validação

```http
400 Bad Request
```

```json
{
  "correlationId": "4a11b94c-45b7-4a48-9cb4-917ecf2c7f31",
  "code": "VALIDATION_ERROR",
  "message": "A requisição possui campos inválidos.",
  "details": [
    {
      "field": "amount",
      "message": "O valor deve ser maior que zero."
    }
  ]
}
```

### Recurso Não Encontrado

```http
404 Not Found
```

```json
{
  "correlationId": "4a11b94c-45b7-4a48-9cb4-917ecf2c7f31",
  "code": "RESOURCE_NOT_FOUND",
  "message": "Recurso não encontrado."
}
```

### Erro Interno

```http
500 Internal Server Error
```

```json
{
  "correlationId": "4a11b94c-45b7-4a48-9cb4-917ecf2c7f31",
  "code": "INTERNAL_ERROR",
  "message": "Erro interno ao processar a solicitação."
}
```

## Eventos Entre Componentes

Após a criação de um lançamento, a API de Lançamentos deve publicar um evento para permitir a consolidação assíncrona.

Na implementação atual, esse evento é publicado no RabbitMQ com a seguinte configuração local:

| Item | Valor |
| --- | --- |
| Exchange | `cash-flow.events` |
| Queue | `cash-flow.entry-created` |
| Routing key | `entry.created` |

A API de lançamentos também pode registrar uma cópia local temporária do evento em arquivo JSON quando executada em modo de armazenamento em arquivo. Esse modo existe apenas para apoio de desenvolvimento sem PostgreSQL/RabbitMQ.

### Evento: EntryCreated

```json
{
  "eventUid": "ad6afde9-9d36-4a79-b6c7-7314ad03b281",
  "correlationId": "4a11b94c-45b7-4a48-9cb4-917ecf2c7f31",
  "eventType": "EntryCreated",
  "occurredAt": "2026-09-01T10:15:31Z",
  "data": {
    "entryUid": "0f0b8b8d-5022-4b62-9e38-7b6f6a87f121",
    "type": "CREDIT",
    "amount": 150.75,
    "entryDate": "2026-09-01"
  }
}
```

## Evoluções Para Produção

- Os endpoints documentados representam os contratos atuais da implementação incremental.
- A autenticação local por API Key deve evoluir para uma estratégia corporativa antes de produção.
- Paginação, filtros adicionais e ordenação podem ser adicionados conforme o volume e os requisitos de consulta aumentarem.
- A consistência eventual deve ser considerada na consulta do saldo consolidado.
