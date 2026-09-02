# ADR 0002 - Processar Consolidação de Forma Assíncrona

## Status

Aceita

## Contexto

A consolidação diária depende dos lançamentos financeiros registrados ao longo do dia. Como o sistema precisa continuar aceitando lançamentos mesmo se a consolidação estiver indisponível, o processamento do saldo não deve bloquear o fluxo principal de registro.

Também é necessário considerar que picos de lançamentos podem ocorrer em determinados horários. Se a consolidação for executada de forma síncrona durante o registro, o tempo de resposta da operação principal pode aumentar.

## Decisão

Planejar a consolidação diária como um processamento assíncrono.

Após registrar um lançamento, a solução deve publicar uma informação de alteração para que a consolidação seja processada separadamente. A API de Lançamentos não deve depender da conclusão da consolidação para responder ao comerciante.

Nesta etapa da implementação, a API grava eventos `EntryCreated` em uma Outbox transacional no PostgreSQL. Uma rotina em segundo plano publica esses eventos no RabbitMQ usando:

- exchange `cash-flow.events`;
- fila `cash-flow.entry-created`;
- routing key `entry.created`.

O worker `cash-flow-consolidation-worker` roda a partir do projeto `CashFlowArchitecture.Worker`, consome a fila `cash-flow.entry-created` e atualiza o saldo diário consolidado no PostgreSQL.

A API mantém uma cópia local temporária dos eventos quando executada no modo de armazenamento em arquivo, permitindo o processamento manual pelo endpoint `POST /daily-balances/process-events` durante a evolução do desafio.

## Consequências Positivas

- Mantém o registro de lançamentos desacoplado do cálculo de saldo.
- Reduz o impacto de falhas temporárias na consolidação.
- Permite reprocessamento de eventos pendentes.
- Favorece escalabilidade do processamento de consolidação.
- Melhora a previsibilidade do tempo de resposta da criação de lançamentos.

## Consequências Negativas

- A consulta de saldo pode apresentar consistência eventual.
- Será necessário monitorar falhas no processamento assíncrono.
- A solução precisará prever reprocessamento e tratamento de duplicidade.
- A arquitetura fica mais complexa do que um fluxo totalmente síncrono.
- A rotina de publicação da Outbox precisa de política avançada de retry, fila de erro e limpeza de mensagens antigas em uma evolução produtiva.

## Alternativas Consideradas

### Consolidar saldo durante o registro do lançamento

Essa alternativa simplificaria o fluxo inicial, mas faria a API de Lançamentos depender diretamente da atualização do saldo. Isso contraria o requisito de manter lançamentos disponíveis mesmo quando a consolidação estiver indisponível.

### Consolidar saldo apenas por agendamento diário

Essa alternativa reduziria a quantidade de processamento durante o dia, mas deixaria o saldo consolidado defasado até a execução do agendamento. Para consultas frequentes durante o expediente, a experiência seria pior.
