# ADR 0008 - Controlar retentativas da Outbox

## Status

Aceita

## Contexto

A Outbox reduz o risco de perda de evento entre a gravação do lançamento e a publicação no RabbitMQ. Porém, se a publicação falhar repetidamente, a rotina não deve tentar publicar a mesma mensagem sem controle.

Sem uma política explícita, erros temporários podem gerar pressão desnecessária no RabbitMQ e erros permanentes podem ficar presos indefinidamente como mensagens pendentes.

## Decisão

Controlar as retentativas de publicação da Outbox com campos operacionais na tabela `outbox_messages`.

Regras adotadas:

- mensagens novas ficam disponíveis para publicação imediatamente;
- em caso de falha temporária, `retryCount` é incrementado;
- a próxima tentativa é agendada em `nextAttemptAt`;
- a mensagem deixa de ser selecionada enquanto `nextAttemptAt` estiver no futuro;
- após atingir o limite de tentativas, a mensagem recebe `failedAt`;
- mensagens com `failedAt` não são republicadas automaticamente;
- mensagens publicadas com sucesso recebem `processedAt`.

Parâmetros locais iniciais:

| Parâmetro | Valor |
| --- | --- |
| Lote de publicação | 20 mensagens |
| Intervalo do publicador | 5 segundos |
| Atraso entre retentativas | 30 segundos |
| Limite de tentativas | 5 |

Esses valores são configuráveis por ambiente.

## Consequências Positivas

- Evita retry agressivo em caso de indisponibilidade do RabbitMQ.
- Diferencia mensagem pendente, mensagem aguardando nova tentativa, mensagem publicada e mensagem com falha definitiva.
- Facilita diagnóstico pelo Adminer.
- Cria base para métricas e alertas de mensagens com erro.

## Consequências Negativas

- Aumenta a quantidade de campos operacionais na Outbox.
- Exige procedimento futuro para reprocessar ou tratar mensagens com `failedAt`.
- Ainda não substitui uma fila de erro dedicada em uma solução produtiva.

## Alternativas Consideradas

### Tentar publicar indefinidamente a cada ciclo

Foi descartada porque pode gerar ruído operacional e pressão desnecessária na mensageria durante falhas prolongadas.

### Enviar diretamente para uma fila de erro

Foi considerada, mas deixada como evolução futura. Para o desafio atual, o estado `failedAt` no PostgreSQL é suficiente para demonstrar a decisão arquitetural e manter a execução local simples.

### Usar backoff exponencial

Foi considerado, mas não aplicado nesta etapa para manter o comportamento simples de explicar e validar. Pode ser adotado se o volume ou a criticidade operacional aumentarem.

## Evolução Futura

Em uma evolução produtiva, a política pode avançar para:

- backoff exponencial;
- jitter para evitar rajadas de retry;
- fila de erro dedicada;
- endpoint ou comando administrativo para republicar mensagens com falha;
- limpeza de mensagens processadas antigas;
- métricas e alertas para mensagens com `failedAt`.
