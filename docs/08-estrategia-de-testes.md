# Estratégia de Testes

Este documento descreve a estratégia inicial de testes para a solução. O objetivo é orientar a futura implementação, garantindo que as regras principais do domínio, os contratos de API e os comportamentos de resiliência sejam validados.

## Objetivos

A estratégia de testes deve garantir que:

- lançamentos de crédito e débito sejam registrados corretamente;
- valores inválidos sejam rejeitados;
- o saldo diário seja calculado corretamente;
- a consolidação não bloqueie o registro de lançamentos;
- eventos duplicados não gerem saldo duplicado;
- falhas de consolidação possam ser diagnosticadas e reprocessadas;
- contratos públicos não exponham IDs internos do banco de dados.
- `correlationId` seja retornado e propagado entre API, eventos e consolidação.

## Pirâmide de Testes

```text
                 Testes end-to-end
              Testes de integração
         Testes de contrato e API
             Testes unitários
```

A maior parte dos testes deve ficar nos níveis unitário, contrato/API e integração. Testes end-to-end devem existir apenas para os fluxos críticos.

## Testes Unitários

Testes unitários devem validar regras isoladas do domínio e casos de cálculo.

| Área | Cenários |
| --- | --- |
| Lançamentos | Criar crédito válido, criar débito válido, rejeitar valor menor ou igual a zero, rejeitar tipo inválido. |
| Consolidação | Somar créditos, somar débitos, calcular saldo final, consolidar data sem lançamentos. |
| Identificação | Gerar `uid` público, não depender de `id` interno em regras públicas. |
| Idempotência | Ignorar evento já processado ou garantir que reprocessamento não duplique saldo. |

## Testes de API

Testes de API devem validar contratos HTTP, payloads e códigos de resposta.

| Endpoint | Cenários |
| --- | --- |
| `POST /entries` | Retornar `201 Created` para lançamento válido. |
| `POST /entries` | Retornar `400 Bad Request` para valor inválido. |
| `POST /entries` | Retornar `400 Bad Request` para tipo inválido. |
| `GET /entries?date=YYYY-MM-DD` | Retornar lançamentos da data solicitada. |
| `GET /daily-balances/{date}` | Retornar `200 OK` quando o saldo estiver consolidado. |
| `GET /daily-balances/{date}` | Retornar `202 Accepted` quando o saldo ainda estiver pendente. |

As respostas públicas devem retornar `uid`, `entryUid` ou `eventUid` quando necessário. O campo `id` interno não deve aparecer em respostas públicas.

Também deve ser validado que as respostas retornam `correlationId` e que a API respeita o valor recebido no header `X-Correlation-Id`.

## Testes de Integração

Testes de integração devem validar a comunicação entre componentes.

| Integração | Cenários |
| --- | --- |
| API de Lançamentos e base de lançamentos | Persistir lançamento válido e permitir consulta posterior. |
| API de Lançamentos e canal de eventos | Publicar evento após criação de lançamento. |
| Processador de Consolidação e canal de eventos | Consumir evento e atualizar saldo da data. |
| Processador de Consolidação e base de saldos | Persistir totais de crédito, débito e saldo final. |
| Rastreabilidade entre componentes | Propagar o mesmo `correlationId` da requisição para o evento e para os logs de consolidação. |

## Testes de Resiliência

Testes de resiliência devem validar o comportamento da solução em falhas previstas.

| Cenário | Resultado Esperado |
| --- | --- |
| Consolidação indisponível | API de Lançamentos continua registrando novos lançamentos. |
| Evento processado mais de uma vez | Saldo consolidado não é duplicado. |
| Falha ao persistir saldo | Erro é registrado e evento pode ser reprocessado. |
| Atraso no processamento | Saldo pode ficar pendente, mas o atraso deve ser observável. |

## Testes End-to-End

Testes end-to-end devem cobrir apenas jornadas essenciais.

### Jornada: Registrar lançamento e consultar saldo

1. Criar lançamento de crédito.
2. Criar lançamento de débito.
3. Aguardar consolidação.
4. Consultar saldo diário.
5. Validar total de créditos, total de débitos e saldo final.

### Jornada: Consolidação indisponível

1. Simular indisponibilidade do processador de consolidação.
2. Criar novo lançamento.
3. Validar que o lançamento foi registrado.
4. Validar que o saldo ainda não foi atualizado.
5. Retomar consolidação.
6. Validar que o saldo foi atualizado posteriormente.

## Critérios de Qualidade

| Critério | Expectativa |
| --- | --- |
| Clareza | Testes devem descrever comportamento de negócio, não detalhes internos. |
| Isolamento | Testes unitários não devem depender de banco, rede ou mensageria real. |
| Reprodutibilidade | Testes devem produzir o mesmo resultado em execuções repetidas. |
| Rastreabilidade | Falhas devem indicar claramente qual comportamento foi quebrado. |
| Segurança do contrato | APIs públicas não devem expor IDs internos. |

## Pontos Para Evolução

- Definir ferramentas de teste conforme a stack escolhida.
- Definir estratégia de dados de teste.
- Definir cobertura mínima esperada.
- Definir testes automatizados em pipeline de CI.
- Definir testes de carga para endpoints críticos.
