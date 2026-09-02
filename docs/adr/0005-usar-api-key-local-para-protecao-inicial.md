# ADR 0005 - Usar API Key local para proteção inicial

## Status

Aceita

## Contexto

As APIs expõem endpoints de negócio para criação e consulta de lançamentos financeiros, além de consulta e processamento de saldo diário.

Mesmo em um desafio técnico, esses endpoints não devem ficar totalmente abertos, porque isso enfraquece a leitura de segurança da solução. Ao mesmo tempo, implementar uma solução completa de identidade, autorização por escopo, emissão de tokens e integração com provedor externo aumentaria o escopo sem ser o foco principal do desafio.

O objetivo desta etapa é demonstrar que a arquitetura considera autenticação desde o início, mantendo a implementação proporcional ao tamanho da solução.

## Decisão

Proteger os endpoints de negócio usando o header `X-Api-Key`.

Endpoints protegidos:

| Serviço | Endpoint | Regra |
| --- | --- | --- |
| API de Lançamentos | `POST /entries` | Exige `X-Api-Key`. |
| API de Lançamentos | `GET /entries?date=YYYY-MM-DD` | Exige `X-Api-Key`. |
| API de Saldo Consolidado | `POST /daily-balances/process-events` | Exige `X-Api-Key`. |
| API de Saldo Consolidado | `GET /daily-balances/{date}` | Exige `X-Api-Key`. |

Endpoints públicos:

| Endpoint | Motivo |
| --- | --- |
| `GET /health` | Permite verificar disponibilidade básica da aplicação. |
| `GET /health/live` | Permite verificar se o processo da API está vivo. |
| `GET /health/ready` | Permite verificar se a API está pronta para operar com suas dependências. |
| `/swagger` | Permite testar a API em ambiente de desenvolvimento. |

A chave local padrão fica documentada em `.env.example` e no `README.md`, apenas para desenvolvimento.

## Consequências Positivas

- Evita que endpoints de negócio fiquem totalmente públicos.
- Mantém a implementação simples e adequada ao estágio atual do desafio.
- Permite testar autenticação diretamente pelo Swagger.
- Cria base para evoluir a segurança sem alterar os contratos principais da API.
- Permite adicionar testes automatizados para chamadas autorizadas e não autorizadas.

## Consequências Negativas

- API Key não representa uma solução completa de autenticação e autorização para produção.
- Não há identidade de usuário final.
- Não há escopos, perfis, claims ou autorização por recurso.
- A rotação e gestão segura da chave ainda não estão modeladas.

## Alternativas Consideradas

### Manter endpoints sem autenticação

Foi descartada porque deixaria uma lacuna clara de segurança nos contratos de negócio.

### Implementar OAuth2, OpenID Connect ou JWT imediatamente

Foi descartada nesta etapa por aumentar o escopo técnico do desafio. Essa abordagem seria mais adequada para uma evolução posterior, quando houver definição de provedor de identidade, escopos, usuários, clientes e políticas de autorização.

### Usar autenticação apenas no API Gateway

Foi considerada como evolução futura. Para o desafio local, a API precisa ser executável e testável sem depender de infraestrutura externa.

## Evolução Futura

Em uma solução produtiva, a autenticação deve evoluir para uma estratégia mais robusta, como:

- OAuth2 ou OpenID Connect com provedor de identidade;
- JWT validado pela API ou por API Gateway;
- autenticação serviço-a-serviço para integrações internas;
- autorização por escopo, perfil ou recurso;
- gestão segura de secrets em cofre de segredos;
- rotação periódica de credenciais;
- auditoria de acessos negados e autorizados.

Essa evolução deve substituir a API Key local, não apenas complementá-la.
