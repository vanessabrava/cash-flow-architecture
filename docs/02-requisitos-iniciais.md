# Requisitos Iniciais

Este documento registra uma primeira leitura dos requisitos do desafio. A lista ainda será refinada nas próximas etapas, conforme a arquitetura evoluir.

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
| RNF-002 | A solução deve permitir evolução para processamento assíncrono da consolidação diária. |
| RNF-003 | A solução deve registrar informações suficientes para rastrear operações importantes. |
| RNF-004 | A solução deve permitir validação automatizada por testes. |
| RNF-005 | A arquitetura deve favorecer separação de responsabilidades entre lançamento e consolidação. |

## Dados Iniciais de Um Lançamento

Um lançamento financeiro deve conter, no mínimo:

- Identificador.
- Tipo do lançamento: crédito ou débito.
- Valor.
- Data do lançamento.
- Descrição.
- Data de criação do registro.

## Observações

Os requisitos acima representam uma base inicial para discussão arquitetural. Eles ainda não definem detalhes de API, tecnologia, banco de dados, mensageria ou estratégia de deploy.
