# ADR 0003 - Usar Idempotency-Key na Criação de Lançamentos

## Status

Aceita

## Contexto

A API de Lançamentos recebe requisições `POST /entries` para registrar créditos e débitos.

Uma requisição `POST` pode ser repetida pelo consumidor por vários motivos:

- timeout na resposta;
- instabilidade de rede;
- retry automático do cliente;
- clique duplicado em uma interface;
- reenvio manual sem perceber que a primeira tentativa foi concluída.

Sem um mecanismo de idempotência, duas requisições com o mesmo objetivo de negócio podem criar dois lançamentos diferentes. Ao mesmo tempo, a solução não pode simplesmente impedir lançamentos com mesmo valor, tipo, descrição e data, porque duas vendas iguais no mesmo dia podem ser legítimas.

## Decisão

A criação de lançamentos deve aceitar o header `Idempotency-Key`.

Quando o consumidor enviar uma `Idempotency-Key`, a API deve tratar a combinação entre consumidor e chave de idempotência como uma única tentativa lógica de criação.

Regras esperadas:

- Se a chave ainda não tiver sido usada, a API cria o lançamento normalmente.
- Se a mesma chave for enviada novamente para a mesma operação, a API retorna o lançamento já criado.
- A repetição da mesma chave não deve criar novo lançamento.
- A chave deve ter validade operacional limitada, a ser definida na implementação final.
- A ausência do header mantém o comportamento padrão: cada `POST /entries` representa uma nova tentativa de criação.

Essa decisão não substitui a idempotência da consolidação. São controles diferentes:

- `Idempotency-Key` protege a criação do lançamento contra duplicidade causada por retry do consumidor.
- `eventUid` protege a consolidação contra aplicação duplicada do mesmo evento.

## Consequências Positivas

- Reduz duplicidade acidental de lançamentos.
- Melhora a segurança operacional em cenários de timeout e retry.
- Permite que clientes façam retry sem medo de criar lançamentos duplicados.
- Preserva a possibilidade de existirem lançamentos legítimos com dados iguais.

## Consequências Negativas

- Exige persistir o histórico das chaves de idempotência.
- Exige política de retenção para não manter chaves indefinidamente.
- Exige cuidado para não reutilizar a mesma chave em operações diferentes.
- A implementação precisa definir o comportamento quando a mesma chave for reutilizada com payload diferente.

## Alternativas Consideradas

### Bloquear lançamentos com mesmo conteúdo

Essa alternativa rejeitaria lançamentos com mesmo tipo, valor, descrição e data.

Foi descartada porque dados iguais não significam necessariamente duplicidade. Um comerciante pode ter duas vendas reais com o mesmo valor no mesmo dia.

### Depender apenas do UID gerado pela API

Essa alternativa mantém cada requisição criando um novo UID.

Foi descartada para proteção contra retry, porque o UID só existe depois da criação. Se o consumidor não receber a resposta por timeout, ele não tem como saber se a operação foi concluída.

### Usar apenas correlationId

Essa alternativa reutilizaria o `X-Correlation-Id` para evitar duplicidade.

Foi descartada porque `correlationId` serve para rastreabilidade técnica da jornada, não para definir unicidade de uma operação de negócio. O mesmo `correlationId` pode aparecer em múltiplas chamadas relacionadas.
