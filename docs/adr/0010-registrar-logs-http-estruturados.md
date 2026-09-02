# ADR 0010 - Registrar logs HTTP estruturados

## Status

Aceita

## Contexto

A solução já propaga `correlationId` nas respostas, eventos e processamento assíncrono. Para apoiar diagnóstico operacional, também é necessário registrar as requisições HTTP de forma estruturada.

Logs de entrada e saída ajudam a responder perguntas comuns durante suporte:

- qual endpoint foi chamado;
- qual status HTTP foi retornado;
- quanto tempo a requisição levou;
- qual `correlationId` deve ser usado para rastrear a jornada.

## Decisão

Adicionar um middleware de observabilidade HTTP na API.

Esse middleware:

- obtém ou gera o `correlationId` no início da requisição;
- escreve o `X-Correlation-Id` na resposta;
- usa o mesmo valor no `TraceIdentifier` da requisição;
- registra método HTTP, rota, status code, duração e `correlationId`;
- registra falhas não tratadas como erro;
- não registra payload, senha, API Key ou outros dados sensíveis.

## Consequências Positivas

- Melhora a rastreabilidade de chamadas HTTP.
- Padroniza o `correlationId` antes dos endpoints de negócio.
- Ajuda a diagnosticar lentidão e falhas por endpoint.
- Mantém logs úteis sem expor dados sensíveis.

## Consequências Negativas

- Aumenta o volume de logs gerado pela API.
- Ainda não substitui tracing distribuído completo.
- Ainda não define dashboards ou métricas agregadas.

## Alternativas Consideradas

### Registrar logs diretamente em cada endpoint

Foi descartada porque duplicaria lógica e aumentaria a chance de inconsistência entre endpoints.

### Adotar uma biblioteca completa de observabilidade agora

Foi considerada, mas deixada para evolução futura. Para esta etapa do desafio, um middleware simples atende à necessidade de rastreabilidade sem aumentar demais o escopo.

## Evolução Futura

Em uma evolução produtiva, a observabilidade pode avançar para:

- logs em formato JSON padronizado;
- tracing distribuído com OpenTelemetry;
- métricas de latência por rota e status code;
- dashboards operacionais;
- alertas por aumento de erro, latência ou degradação.
