# CashFlow

Desafio técnico para registro de lançamentos financeiros de crédito e
débito e consulta do saldo consolidado diário.

A solução foi desenvolvida em .NET 10 e separa o registro das
movimentações do processamento do consolidado. A API persiste os
lançamentos e a Transactional Outbox; o Worker publica os eventos no
RabbitMQ, consome as mensagens e mantém o consolidado diário previamente
calculado.

Este README também registra os principais comandos utilizados para
montar, executar e validar o projeto.

## Arquitetura

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

Responsabilidades:

-   `CashFlow.Domain`: entidades e regras de negócio.
-   `CashFlow.Application`: casos de uso e contratos.
-   `CashFlow.Infrastructure`: EF Core, MySQL, repositórios, Outbox e
    integração com RabbitMQ.
-   `CashFlow.Api`: endpoints HTTP, tratamento de erros e composição da
    aplicação.
-   `CashFlow.Worker`: publicação da Outbox, consumo das mensagens e
    atualização do consolidado.
-   `CashFlow.Domain.Tests` e `CashFlow.Application.Tests`: testes
    automatizados.

A API funciona como Composition Root e registra as dependências de
Application e Infrastructure.

## Fluxo principal

``` text
POST /api/entries
        ↓
CreateEntryUseCase
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
        ↓
ACK
```

A consulta utiliza o consolidado previamente processado:

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
mesmo se o RabbitMQ ou o processamento do consolidado estiverem
temporariamente indisponíveis.

## Tecnologias

-   .NET 10
-   ASP.NET Core
-   OpenAPI
-   Scalar
-   Entity Framework Core 9
-   Pomelo Entity Framework Core para MySQL
-   MySQL 8.4
-   RabbitMQ
-   xUnit
-   Docker Compose

## Pré-requisitos

-   .NET 10 SDK
-   Docker Desktop ou Docker Engine com Docker Compose
-   ferramenta `dotnet-ef`, caso ainda não esteja instalada

Para instalar `dotnet-ef`:

``` powershell
dotnet tool install --global dotnet-ef
```

Para conferir o ambiente:

``` powershell
dotnet --version
docker --version
docker compose version
dotnet ef --version
```

# 1. Infraestrutura local

## Docker Compose

O arquivo `docker-compose.yml` utilizado pelo projeto contém MySQL e
RabbitMQ:

``` yaml
services:
  mysql:
    image: mysql:8.4
    container_name: cashflow-mysql
    restart: unless-stopped
    environment:
      MYSQL_ROOT_PASSWORD: root
      MYSQL_DATABASE: cashflow
    ports:
      - "3306:3306"
    volumes:
      - cashflow-mysql-data:/var/lib/mysql
    healthcheck:
      test: ["CMD", "mysqladmin", "ping", "-h", "localhost", "-proot"]
      interval: 10s
      timeout: 5s
      retries: 5

  rabbitmq:
    image: rabbitmq:4-management
    container_name: cashflow-rabbitmq
    restart: unless-stopped
    ports:
      - "5674:5672"
      - "15674:15672"
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    volumes:
      - rabbitmq-data:/var/lib/rabbitmq

volumes:
  cashflow-mysql-data:
  rabbitmq-data:
```

O RabbitMQ usa portas externas diferentes das portas padrão porque
outros projetos locais já utilizavam as portas padrão.

``` text
MySQL:                 localhost:3306
RabbitMQ AMQP:         localhost:5674
RabbitMQ Management:   localhost:15674
```

Credenciais utilizadas apenas no ambiente local do desafio:

``` text
MySQL
Usuário: root
Senha:   root
Banco:   cashflow

RabbitMQ
Usuário: guest
Senha:   guest
```

## Subindo MySQL e RabbitMQ

Na raiz do repositório:

``` powershell
docker compose up -d
```

Validar os containers:

``` powershell
docker compose ps
```

Ver os containers em execução:

``` powershell
docker ps
```

A interface de gerenciamento do RabbitMQ fica disponível em:

``` text
http://localhost:15674
```

## Parando a infraestrutura

``` powershell
docker compose down
```

Para remover também os volumes e recriar o ambiente do zero:

``` powershell
docker compose down -v
docker compose up -d
```

> `docker compose down -v` apaga os dados locais persistidos nos
> volumes.

# 2. Banco de dados e migrations

A persistência utiliza MySQL 8.4 com Entity Framework Core.

As principais tabelas são:

``` text
entries
outbox_messages
daily_balances
processed_messages
__EFMigrationsHistory
```

## Migration inicial

O comando utilizado para criar a primeira migration foi:

``` powershell
dotnet ef migrations add InitialCreate `
    --project CashFlow.Infrastructure `
    --startup-project CashFlow.Api `
    --output-dir Persistence/Migrations
```

Aplicação da migration:

``` powershell
dotnet ef database update `
    --project CashFlow.Infrastructure `
    --startup-project CashFlow.Api
```

## Migration da Transactional Outbox

``` powershell
dotnet ef migrations add AddOutboxMessages `
    --project CashFlow.Infrastructure `
    --startup-project CashFlow.Api
```

``` powershell
dotnet ef database update `
    --project CashFlow.Infrastructure `
    --startup-project CashFlow.Api
```

## Migration do consolidado diário

``` powershell
dotnet ef migrations add AddDailyBalances `
    --project CashFlow.Infrastructure `
    --startup-project CashFlow.Api
```

``` powershell
dotnet ef database update `
    --project CashFlow.Infrastructure `
    --startup-project CashFlow.Api
```

## Migration de mensagens processadas

``` powershell
dotnet ef migrations add AddProcessedMessages `
    --project CashFlow.Infrastructure `
    --startup-project CashFlow.Api
```

``` powershell
dotnet ef database update `
    --project CashFlow.Infrastructure `
    --startup-project CashFlow.Api
```

Em uma instalação nova não é necessário recriar cada migration. Basta
executar:

``` powershell
dotnet ef database update `
    --project CashFlow.Infrastructure `
    --startup-project CashFlow.Api
```

O EF Core aplicará as migrations existentes na ordem correta.

## Inspecionando o banco

Listar tabelas:

``` powershell
docker exec cashflow-mysql mysql -uroot -proot cashflow -e "SHOW TABLES;"
```

Conferir a tabela de lançamentos:

``` powershell
docker exec cashflow-mysql mysql -uroot -proot cashflow -e "DESCRIBE entries;"
```

Conferir a Outbox:

``` powershell
docker exec cashflow-mysql mysql -uroot -proot cashflow -e "DESCRIBE outbox_messages;"
```

Conferir o consolidado:

``` powershell
docker exec cashflow-mysql mysql -uroot -proot cashflow -e "DESCRIBE daily_balances;"
```

Conferir a tabela de idempotência:

``` powershell
docker exec cashflow-mysql mysql -uroot -proot cashflow -e "DESCRIBE processed_messages;"
```

Consultar dados:

``` powershell
docker exec cashflow-mysql mysql -uroot -proot cashflow -e "SELECT * FROM entries;"
```

``` powershell
docker exec cashflow-mysql mysql -uroot -proot cashflow -e "SELECT * FROM outbox_messages;"
```

``` powershell
docker exec cashflow-mysql mysql -uroot -proot cashflow -e "SELECT * FROM daily_balances;"
```

``` powershell
docker exec cashflow-mysql mysql -uroot -proot cashflow -e "SELECT * FROM processed_messages;"
```

# 3. Compilando e testando

Na raiz da solução:

``` powershell
dotnet build
```

Executar os testes:

``` powershell
dotnet test
```

Resultado validado ao final do desenvolvimento:

``` text
Total: 19
Aprovados: 19
Falhas: 0
```

# 4. Executando a API

``` powershell
dotnet run --project CashFlow.Api
```

Durante o desenvolvimento:

``` text
http://localhost:5192
```

## OpenAPI e Scalar

A API utiliza OpenAPI e, em ambiente Development, disponibiliza uma
interface interativa através do Scalar:

``` text
http://localhost:5192/scalar/v1
```

Por ela é possível visualizar os contratos e executar os endpoints sem
Postman ou PowerShell.

# 5. Executando o Worker

Em outro terminal:

``` powershell
dotnet run --project CashFlow.Worker
```

O Worker executa duas responsabilidades:

1.  `OutboxPublisherService`: lê mensagens pendentes em
    `outbox_messages` e publica no RabbitMQ.
2.  `DailyBalanceConsumerService`: consome os eventos, atualiza
    `daily_balances` e registra o `EntryId` em `processed_messages`.

# 6. Endpoints e payloads de teste

## Tipos de lançamento

``` text
1 = Crédito
2 = Débito
```

O valor `0` não é válido.

## Criar um crédito

Endpoint:

``` http
POST /api/entries
```

Payload utilizado na validação pelo Scalar:

``` json
{
  "type": 1,
  "amount": 100,
  "description": "Teste Scalar",
  "occurredAt": "2026-09-02T15:00:00Z"
}
```

Resultado esperado:

``` text
201 Created
```

Exemplo equivalente em PowerShell:

``` powershell
$body = @{
    type = 1
    amount = 100
    description = "Teste Scalar"
    occurredAt = "2026-09-02T15:00:00Z"
} | ConvertTo-Json

Invoke-RestMethod `
    -Uri "http://localhost:5192/api/entries" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body
```

## Criar um débito

Payload utilizado na validação:

``` json
{
  "type": 2,
  "amount": 75,
  "description": "Teste debito Scalar",
  "occurredAt": "2026-09-02T15:30:00Z"
}
```

PowerShell:

``` powershell
$body = @{
    type = 2
    amount = 75
    description = "Teste debito Scalar"
    occurredAt = "2026-09-02T15:30:00Z"
} | ConvertTo-Json

Invoke-RestMethod `
    -Uri "http://localhost:5192/api/entries" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body
```

Resultado esperado:

``` text
201 Created
```

## Consultar o consolidado

``` http
GET /api/daily-balances/{date}
```

PowerShell:

``` powershell
Invoke-RestMethod `
    -Uri "http://localhost:5192/api/daily-balances/2026-09-02" `
    -Method Get
```

Na validação final pelo Scalar, após o processamento dos lançamentos de
crédito e débito, o resultado foi:

``` json
{
  "date": "2026-09-02",
  "totalCredits": 550.00,
  "totalDebits": 225.00,
  "balance": 325.00
}
```

Para uma data sem movimentações, a API retorna o consolidado zerado.

# 7. Transactional Outbox

O `POST /api/entries` não publica diretamente no RabbitMQ.

O lançamento e a mensagem `EntryCreated` são adicionados ao mesmo
`DbContext` e persistidos por um único `SaveChangesAsync`.

``` text
POST
 ↓
Entry
 ↓
entries + outbox_messages
 ↓
COMMIT
 ↓
201 Created
```

Isso evita que uma indisponibilidade do RabbitMQ faça o registro
financeiro falhar.

Se o Worker estiver parado, o lançamento continua salvo e a mensagem
permanece pendente na Outbox.

É possível conferir as mensagens com:

``` powershell
docker exec cashflow-mysql mysql -uroot -proot cashflow -e "SELECT Id, Type, OccurredAt, ProcessedAt FROM outbox_messages ORDER BY OccurredAt;"
```

Mensagens ainda não publicadas possuem:

``` text
ProcessedAt = NULL
```

# 8. RabbitMQ

O publisher utiliza:

``` text
Exchange:    cashflow.entries
Routing key: entry.created
Queue:       cashflow.daily-balance
```

Para verificar a fila:

``` powershell
docker exec cashflow-rabbitmq rabbitmqctl list_queues name messages consumers
```

Com o Worker executando e a fila processada, é esperado algo semelhante
a:

``` text
cashflow.daily-balance  0  1
```

onde não há mensagens aguardando e existe um consumidor conectado.

# 9. Consolidação assíncrona

O consumidor recebe o `EntryCreatedEvent` e:

``` text
1. verifica se o EntryId já foi processado
2. determina o dia do lançamento em UTC
3. localiza ou cria daily_balances
4. soma em crédito ou débito
5. registra EntryId em processed_messages
6. salva as alterações
7. envia ACK ao RabbitMQ
```

A mensagem só recebe `ACK` após a persistência.

Em caso de erro, o consumidor envia `NACK` com requeue.

# 10. Consistência eventual

O processamento do consolidado é assíncrono.

Portanto, imediatamente depois de:

``` http
POST /api/entries
```

é possível que:

``` http
GET /api/daily-balances/{date}
```

ainda retorne o estado anterior por alguns instantes.

Esse comportamento foi observado durante a validação pelo Scalar: os
lançamentos foram registrados com `201 Created`, o GET inicialmente
mostrou o consolidado anterior e, após o Worker processar as mensagens,
passou a retornar:

``` text
Créditos: 550,00
Débitos:  225,00
Saldo:    325,00
```

# 11. Validando independência da API

Para demonstrar que o registro financeiro não depende da consolidação:

1.  deixe MySQL e RabbitMQ ativos;
2.  pare o Worker;
3.  execute um `POST /api/entries`;
4.  confirme o `201 Created`;
5.  consulte `entries` e `outbox_messages`;
6.  observe que o consolidado ainda não foi atualizado;
7.  inicie novamente o Worker;
8.  consulte o consolidado após o processamento.

Comando para iniciar novamente:

``` powershell
dotnet run --project CashFlow.Worker
```

Esse cenário foi validado durante o desenvolvimento.

# 12. Idempotência

RabbitMQ trabalha com possibilidade de reentrega. Por isso o consumidor
não pode somar o mesmo lançamento duas vezes.

A tabela:

``` text
processed_messages
```

usa `EntryId` como chave primária.

Antes de alterar o consolidado, o consumidor verifica se aquele
lançamento já foi processado.

A atualização de `daily_balances` e a inclusão em `processed_messages`
são persistidas juntas.

Para consultar:

``` powershell
docker exec cashflow-mysql mysql -uroot -proot cashflow -e "SELECT * FROM processed_messages;"
```

Durante o desenvolvimento a idempotência foi validada republicando um
evento já processado. O crédito não foi somado novamente.

# 13. Validações de erro

Exemplos que devem retornar `400 Bad Request`.

Valor inválido:

``` json
{
  "type": 1,
  "amount": 0,
  "description": "Valor invalido",
  "occurredAt": "2026-09-02T15:00:00Z"
}
```

Descrição vazia:

``` json
{
  "type": 1,
  "amount": 100,
  "description": "",
  "occurredAt": "2026-09-02T15:00:00Z"
}
```

Tipo inválido:

``` json
{
  "type": 0,
  "amount": 100,
  "description": "Tipo invalido",
  "occurredAt": "2026-09-02T15:00:00Z"
}
```

O tratamento é centralizado com `IExceptionHandler` e `ProblemDetails`.

# 14. Teste de carga

O endpoint utilizado foi:

``` http
GET /api/daily-balances/2026-09-02
```

Foi realizada uma rajada local de 50 requisições concorrentes.

Resultado:

``` text
Requisições: 50
Sucessos: 50
Falhas: 0
Percentual de falhas: 0%
Tempo da execução: 0,661 s
```

Também foram realizadas 10 rajadas de 50 consultas.

Resultado:

``` text
Requisições: 500
Sucessos: 500
Falhas: 0
Percentual de falhas: 0%
```

O segundo cenário foi usado como teste de estabilidade. Seu tempo total
não foi usado para calcular throughput porque incluía criação dos jobs
do PowerShell e intervalos entre as rodadas.

Os resultados são evidência do comportamento no ambiente local testado e
não representam benchmark ou garantia de capacidade em produção.

# 15. Decisões técnicas

-   Domain sem dependência de EF Core ou MySQL.
-   Application coordena casos de uso e depende de contratos.
-   Infrastructure implementa persistência e mensageria.
-   API é o ponto de composição das dependências.
-   `decimal` para valores monetários.
-   consolidação previamente calculada em `daily_balances`.
-   Transactional Outbox para desacoplar o registro financeiro do
    RabbitMQ.
-   consumidor idempotente para tolerar reentregas.
-   `ACK` somente após persistência.
-   datas do consolidado interpretadas em UTC.
-   `AsNoTracking()` na leitura do consolidado.
-   OpenAPI + Scalar para documentação e testes manuais da API.

# 16. Pontos de evolução

Em um cenário produtivo, conforme necessidade e volume, poderiam ser
adicionados:

-   retry com política definida e Dead Letter Queue;
-   publisher confirms;
-   recuperação automática mais robusta do Worker após indisponibilidade
    do RabbitMQ;
-   controle de concorrência para múltiplos consumidores atualizando a
    mesma data;
-   observabilidade distribuída, métricas e tracing;
-   testes de carga com ferramenta dedicada e ambiente representativo de
    produção.

Esses itens não são apresentados como funcionalidades já implementadas.

# 17. Documentação complementar

Detalhes da evolução e das decisões estão em:

``` text
docs/01-arquitetura-inicial.md
docs/02-persistencia.md
docs/03-api-lancamentos.md
docs/04-consolidacao-diaria.md
docs/05-mensageria-outbox.md
```

## Execução rápida do zero

Para quem acabou de clonar o repositório, a sequência principal é:

``` powershell
docker compose up -d

dotnet ef database update `
    --project CashFlow.Infrastructure `
    --startup-project CashFlow.Api

dotnet build
dotnet test
```

Terminal 1:

``` powershell
dotnet run --project CashFlow.Api
```

Terminal 2:

``` powershell
dotnet run --project CashFlow.Worker
```

Documentação interativa:

``` text
http://localhost:5192/scalar/v1
```

A partir daí, os payloads das seções anteriores podem ser utilizados
para validar crédito, débito e consulta do consolidado.
