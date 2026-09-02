# Requisitos Iniciais

Este documento registra os requisitos principais usados para orientar o desenho e a implementação incremental do desafio.

## Requisitos Funcionais

Requisitos funcionais descrevem o que o sistema precisa fazer.

| ID | Requisito |
| --- | --- |
| RF-001 | O sistema deve permitir registrar lançamentos financeiros de crédito. |
| RF-002 | O sistema deve permitir registrar lançamentos financeiros de débito. |
| RF-003 | O sistema deve armazenar os dados principais de um lançamento financeiro. |
| RF-004 | O sistema deve permitir consultar lançamentos financeiros por data. |
| RF-005 | O sistema deve calcular o saldo diário consolidado. |
| RF-006 | O sistema deve permitir consultar o saldo diário consolidado. |

## Requisitos Não Funcionais

Requisitos não funcionais descrevem qualidades, restrições e condições de operação da solução.

| ID | Requisito |
| --- | --- |
| RNF-001 | O serviço de lançamento não deve ficar indisponível se o serviço de consolidação estiver indisponível. |
| RNF-002 | A solução deve processar a consolidação diária de forma assíncrona. |
| RNF-003 | A solução deve registrar informações suficientes para rastrear operações importantes. |
| RNF-004 | A solução deve permitir validação automatizada por testes. |
| RNF-005 | A arquitetura deve favorecer separação de responsabilidades entre lançamento e consolidação. |
| RNF-006 | A API e o worker devem poder executar em containers separados. |
| RNF-007 | A solução deve evitar duplicidade causada por retry na criação de lançamentos. |
| RNF-008 | A solução deve reduzir o risco de perda de evento entre a gravação do lançamento e a publicação para consolidação. |
| RNF-009 | A API deve expor verificações de liveness e readiness para apoiar operação em containers. |
| RNF-010 | A API deve registrar logs estruturados com `correlationId`, rota, status e duração das requisições. |

## Dados Iniciais de Um Lançamento

Um lançamento financeiro deve conter, no mínimo:

- UID público do lançamento.
- Tipo do lançamento: crédito ou débito.
- Valor.
- Data do lançamento.
- Descrição.
- Data de criação do registro.

## Observações

Os requisitos acima são atendidos parcialmente pela implementação atual. Pontos como autorização, fila de erro, política avançada de retry e observabilidade completa permanecem como evolução planejada.
