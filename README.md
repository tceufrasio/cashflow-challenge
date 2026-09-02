# CashFlow

Desafio técnico para registro de lançamentos financeiros e consulta do
saldo consolidado diário.

A solução foi desenvolvida em .NET 10 e separa o registro das
movimentações do processamento do consolidado. Créditos e débitos são
persistidos pela API, enquanto a consolidação diária é processada de
forma assíncrona pelo Worker através de RabbitMQ.

## Arquitetura

A solução está dividida nos seguintes projetos:

``` text
CashFlow
├── CashFlow.Api
├── CashFlow.Application
├── CashFlow.Domain
├── CashFlow.Infrastructure
├── CashFlow.Worker
├── CashFlow.Domain.Tests
└── CashFlow.Application.Tests
```

Responsabilidades principais:

-   `CashFlow.Domain`: entidades e regras de negócio.
-   `CashFlow.Application`: casos de uso e contratos necessários pela
    aplicação.
-   `CashFlow.Infrastructure`: EF Core, MySQL, repositórios, Outbox e
    integração com RabbitMQ.
-   `CashFlow.Api`: endpoints HTTP e composição da aplicação.
-   `CashFlow.Worker`: publicação da Outbox, consumo das mensagens e
    atualização do consolidado.
-   projetos `Tests`: testes automatizados do domínio e dos casos de
    uso.

A API atua como Composition Root, registrando Application e
Infrastructure através de injeção de dependência.

## Fluxo

O registro de um lançamento e a consolidação possuem responsabilidades
separadas:

``` text
POST /api/entries
        ↓
Application
        ↓
MySQL
 ├── entries
 └── outbox_messages
        ↓
OutboxPublisherService
        ↓
RabbitMQ
        ↓
DailyBalanceConsumerService
        ↓
MySQL
 ├── daily_balances
 └── processed_messages
```

A consulta do consolidado utiliza diretamente o resultado previamente
processado:

``` text
GET /api/daily-balances/{date}
        ↓
GetDailyBalanceUseCase
        ↓
IDailyBalanceRepository
        ↓
daily_balances
```

Essa separação permite que o registro financeiro continue funcionando
mesmo quando o RabbitMQ ou o processamento do consolidado estiverem
temporariamente indisponíveis.

## Tecnologias

-   .NET 10
-   ASP.NET Core
-   Entity Framework Core 9
-   Pomelo Entity Framework Core para MySQL
-   MySQL 8.4
-   RabbitMQ
-   xUnit
-   Docker Compose

## Pré-requisitos

Para executar o projeto localmente:

-   .NET 10 SDK
-   Docker com Docker Compose

## Executando a infraestrutura

Na raiz do projeto:

``` powershell
docker compose up -d
```

O ambiente local utiliza MySQL e RabbitMQ.

Portas documentadas no projeto:

``` text
MySQL:                 3306
RabbitMQ AMQP:         5674
RabbitMQ Management:   15674
```

Para verificar os containers:

``` powershell
docker compose ps
```

## Banco de dados

O schema é versionado através de migrations do Entity Framework Core.

Com a infraestrutura em execução, aplique as migrations:

``` powershell
dotnet ef database update `
    --project CashFlow.Infrastructure `
    --startup-project CashFlow.Api
```

As principais tabelas utilizadas são:

``` text
entries
outbox_messages
daily_balances
processed_messages
```

## Executando a API

Na raiz da solução:

``` powershell
dotnet run --project CashFlow.Api
```

Durante os testes locais a API foi executada em:

``` text
http://localhost:5192
```

## Executando o Worker

Em outro terminal:

``` powershell
dotnet run --project CashFlow.Worker
```

O Worker possui duas responsabilidades principais:

1.  publicar no RabbitMQ as mensagens pendentes da Transactional Outbox;
2.  consumir os eventos de lançamento e atualizar o consolidado diário.

## Endpoints

### Registrar lançamento

``` http
POST /api/entries
```

Exemplo de corpo:

``` json
{
  "type": 0,
  "amount": 100.00,
  "description": "Venda",
  "occurredAt": "2026-09-02T15:00:00Z"
}
```

O domínio trabalha com lançamentos de crédito e débito e exige valor
maior que zero e descrição preenchida.

### Consultar consolidado diário

``` http
GET /api/daily-balances/2026-09-02
```

Exemplo de retorno:

``` json
{
  "date": "2026-09-02",
  "totalCredits": 450.00,
  "totalDebits": 150.00,
  "balance": 300.00
}
```

Quando ainda não existe consolidado para a data solicitada, a consulta
retorna os valores zerados.

## Consistência e resiliência

O registro do lançamento e a mensagem da Outbox são persistidos na mesma
unidade de persistência. Dessa forma, a disponibilidade do RabbitMQ não
determina o sucesso do registro financeiro.

O processamento do consolidado é assíncrono. Portanto, existe uma
pequena janela de consistência eventual entre o retorno do
`POST /api/entries` e a atualização visível em
`GET /api/daily-balances/{date}`.

O consumidor utiliza confirmação manual (`ACK`). A confirmação ocorre
somente depois da persistência do consolidado. Em caso de erro é
utilizado `NACK` com reenvio da mensagem.

Como uma mensagem pode ser entregue novamente, `processed_messages`
utiliza o `EntryId` como chave para impedir que o mesmo lançamento seja
somado duas vezes.

A idempotência foi validada manualmente através da republicação do mesmo
evento, sem alteração duplicada do consolidado.

## Testes

Para executar os testes:

``` powershell
dotnet test
```

Validação final realizada durante o desenvolvimento:

``` text
Total: 19
Aprovados: 19
Falhas: 0
```

Também é possível validar toda a solução com:

``` powershell
dotnet build
```

## Validação de carga

O endpoint de consolidação foi validado localmente com consultas
concorrentes.

Primeiro cenário:

``` text
Requisições: 50
Sucessos: 50
Falhas: 0
Percentual de falhas: 0%
Tempo da execução: 0,661 s
```

Também foram executadas 10 rajadas de 50 consultas:

``` text
Requisições: 500
Sucessos: 500
Falhas: 0
Percentual de falhas: 0%
```

Os resultados demonstram ausência de perda de requisições no ambiente
local e no cenário testado. O teste não deve ser interpretado como
benchmark ou garantia de capacidade de um ambiente produtivo.

## Decisões técnicas

Algumas decisões adotadas durante o desenvolvimento:

-   regras de negócio mantidas no Domain, sem dependência de EF Core;
-   casos de uso dependem de abstrações de persistência;
-   `decimal` é utilizado para valores monetários;
-   consolidação pré-calculada para evitar agregar todos os lançamentos
    em cada consulta;
-   Transactional Outbox para desacoplar o registro financeiro da
    disponibilidade do RabbitMQ;
-   consumidor idempotente para suportar reentrega de mensagens;
-   datas do processamento do consolidado são interpretadas em UTC;
-   `AsNoTracking()` é utilizado na leitura do consolidado.

## Documentação

A evolução e as decisões do projeto estão detalhadas em:

-   `docs/01-arquitetura-inicial.md`
-   `docs/02-persistencia.md`
-   `docs/03-api-lancamentos.md`
-   `docs/04-consolidacao-diaria.md`
-   `docs/05-mensageria-outbox.md`

## Pontos de evolução

Em um ambiente produtivo, alguns pontos poderiam ser evoluídos conforme
a necessidade:

-   política de retry e Dead Letter Queue para mensagens que falham
    permanentemente;
-   publisher confirms e tratamento de indisponibilidade do RabbitMQ na
    inicialização do Worker;
-   estratégia de concorrência para múltiplos consumidores atualizando o
    mesmo consolidado;
-   observabilidade distribuída e métricas;
-   testes de carga com ferramenta dedicada e ambiente representativo de
    produção.

Esses itens foram mantidos fora do escopo atual para evitar complexidade
sem necessidade para o desafio.
