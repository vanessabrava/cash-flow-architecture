# ADR 0007 - Usar Outbox para publicação confiável de eventos

## Status

Aceita

## Contexto

Ao criar um lançamento financeiro, a API precisa persistir o lançamento e emitir o evento `EntryCreated` para que a consolidação diária aconteça de forma assíncrona.

Sem Outbox, existe uma janela de falha entre gravar o lançamento no PostgreSQL e publicar o evento no RabbitMQ. Se a API gravar o lançamento com sucesso, mas falhar antes de publicar o evento, o lançamento fica salvo e a consolidação não recebe a informação.

Esse risco afeta diretamente a confiabilidade da consolidação.

## Decisão

Usar o padrão Outbox para publicação de eventos.

Na criação de lançamento:

1. A API valida a requisição.
2. A API cria o lançamento financeiro.
3. A API cria o evento `EntryCreated`.
4. A API grava lançamento, registro de idempotência e mensagem de Outbox no PostgreSQL.
5. A API confirma a criação ao consumidor.
6. Uma rotina em segundo plano publica mensagens pendentes da Outbox no RabbitMQ.
7. Após publicação com sucesso, a mensagem recebe `processedAt`.

## Consequências Positivas

- Reduz o risco de lançamento salvo sem evento publicado.
- Permite publicar eventos novamente quando RabbitMQ estiver temporariamente indisponível.
- Melhora a rastreabilidade entre banco, evento e consolidação.
- Mantém a API de lançamentos disponível mesmo se a mensageria estiver instável.

## Consequências Negativas

- Adiciona uma tabela operacional ao PostgreSQL.
- Exige rotina de publicação em segundo plano.
- Exige estratégia de limpeza, retry e fila de erro em evolução futura.
- A publicação deixa de ser imediata e passa a ser eventualmente consistente.

## Alternativas Consideradas

### Publicar direto no RabbitMQ após salvar o lançamento

Foi descartada como estratégia principal porque mantém a janela de falha entre persistência e publicação.

### Publicar no RabbitMQ antes de salvar o lançamento

Foi descartada porque poderia gerar evento para um lançamento que depois falhou na persistência.

### Usar transação distribuída entre PostgreSQL e RabbitMQ

Foi descartada por aumentar complexidade operacional e acoplamento entre tecnologias.

## Evolução Futura

Em uma evolução produtiva, a Outbox deve avançar para:

- política de retry com backoff;
- limite de tentativas;
- fila ou estado de erro definitivo;
- limpeza de mensagens antigas já processadas;
- métricas de mensagens pendentes, publicadas e com erro;
- alerta para mensagens paradas por muito tempo;
- separação do publicador de Outbox em worker próprio, caso o volume justifique.
