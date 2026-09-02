# ADR 0001 - Separar Lançamentos e Consolidação

## Status

Aceita

## Contexto

A solução precisa permitir que comerciantes registrem lançamentos financeiros de crédito e débito. Também precisa disponibilizar o saldo diário consolidado.

Um requisito importante é que o registro de lançamentos não fique indisponível caso o serviço de consolidação apresente falha ou indisponibilidade.

Se o registro de lançamentos e a consolidação diária forem tratados como uma única responsabilidade, uma falha no processamento de saldo pode afetar diretamente a operação principal do comerciante: continuar registrando entradas e saídas.

## Decisão

Separar logicamente e operacionalmente o domínio de lançamentos financeiros do domínio de consolidação diária.

O registro de lançamentos será tratado como a fonte principal dos dados financeiros. A consolidação será tratada como uma visão derivada, calculada a partir dos lançamentos registrados.

Na implementação do desafio, a separação aparece em três processos executáveis:

| Serviço | Projeto | Responsabilidade |
| --- | --- | --- |
| API de Lançamentos | `CashFlowArchitecture.Api` | Registrar e consultar lançamentos financeiros. |
| API de Saldo Consolidado | `CashFlowArchitecture.Consolidation.Api` | Consultar saldo diário consolidado. |
| Worker de Consolidação | `CashFlowArchitecture.Worker` | Consumir eventos e atualizar o saldo consolidado. |

No Docker Compose, cada processo sobe em um container separado. Assim, a API de lançamentos pode continuar respondendo mesmo se a API de saldo ou o worker de consolidação estiverem indisponíveis.

## Consequências Positivas

- Reduz o acoplamento entre registro de lançamentos e cálculo de saldo.
- Permite manter o registro de lançamentos disponível mesmo quando a consolidação falhar.
- Facilita reprocessamento do saldo consolidado a partir dos lançamentos originais.
- Torna a solução mais clara para evolução futura em serviços separados.
- Permite demonstrar a independência operacional parando apenas o container de consolidação.

## Consequências Negativas

- A solução passa a ter mais componentes lógicos.
- Será necessário lidar com sincronização entre lançamentos e consolidação.
- A consulta de saldo pode depender de uma visão derivada que precisa ser atualizada corretamente.

## Alternativas Consideradas

### Manter lançamento e consolidação no mesmo componente

Essa alternativa simplificaria a primeira implementação, mas aumentaria o risco de indisponibilidade conjunta. Uma falha no cálculo do saldo poderia afetar o registro de novos lançamentos.

### Calcular saldo sempre sob demanda

Essa alternativa evitaria manter uma base consolidada, mas poderia prejudicar desempenho conforme o volume de lançamentos aumentasse. Também faria a consulta depender de varrer lançamentos históricos com frequência.
