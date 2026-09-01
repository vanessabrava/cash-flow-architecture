# Contratos de API

Este documento descreve uma proposta inicial de contratos HTTP para as capacidades principais da solução. Os contratos ainda não representam uma implementação, mas servem como referência para a arquitetura e para a futura criação do código.

## Convenções

- O formato de troca de dados será JSON.
- Datas serão representadas no formato ISO 8601.
- Valores monetários serão representados como número decimal.
- Identificadores serão representados como texto.
- Nomes de campos de API serão escritos em inglês.

## API de Lançamentos

Responsável por registrar e consultar lançamentos financeiros.

### Criar Lançamento

```http
POST /entries
```

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
  "id": "entry-123",
  "type": "CREDIT",
  "amount": 150.75,
  "description": "Venda no cartão",
  "entryDate": "2026-09-01",
  "createdAt": "2026-09-01T10:15:30Z"
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
  "date": "2026-09-01",
  "items": [
    {
      "id": "entry-123",
      "type": "CREDIT",
      "amount": 150.75,
      "description": "Venda no cartão",
      "entryDate": "2026-09-01",
      "createdAt": "2026-09-01T10:15:30Z"
    },
    {
      "id": "entry-124",
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
  "code": "INTERNAL_ERROR",
  "message": "Erro interno ao processar a solicitação."
}
```

## Eventos Entre Componentes

Após a criação de um lançamento, a API de Lançamentos deve publicar um evento para permitir a consolidação assíncrona.

### Evento: EntryCreated

```json
{
  "eventId": "event-789",
  "eventType": "EntryCreated",
  "occurredAt": "2026-09-01T10:15:31Z",
  "data": {
    "entryId": "entry-123",
    "type": "CREDIT",
    "amount": 150.75,
    "entryDate": "2026-09-01"
  }
}
```

## Observações

- Os nomes dos endpoints ainda podem ser ajustados durante a implementação.
- A autenticação será detalhada em etapa posterior.
- Paginação, filtros adicionais e ordenação serão avaliados conforme a evolução da solução.
- A consistência eventual deve ser considerada na consulta do saldo consolidado.
