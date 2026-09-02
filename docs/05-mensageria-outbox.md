# Mensageria e Transactional Outbox

## Objetivo

A mensageria foi adicionada para desacoplar o registro dos lançamentos do processamento do consolidado diário.

O lançamento financeiro deve continuar sendo registrado mesmo quando o RabbitMQ ou o processamento do consolidado estiverem indisponíveis.

O fluxo implementado é:

```text
API
 ↓
MySQL
 ├── entries
 └── outbox_messages
        ↓
OutboxPublisherService
        ↓
RabbitMQ
        ↓
cashflow.daily-balance
        ↓
DailyBalanceConsumerService
        ↓
MySQL
 ├── daily_balances
 └── processed_messages
```

## RabbitMQ

O RabbitMQ foi incluído no `docker-compose.yml`.

Como outras aplicações locais já utilizavam as portas padrão, foram utilizadas:

- AMQP: `5674`
- Management: `15674`

Para iniciar a infraestrutura:

```powershell
docker compose up -d
```

Para validar:

```powershell
docker compose ps
```

A interface de gerenciamento fica disponível localmente na porta `15674`.

## Transactional Outbox

Foi criada a tabela `outbox_messages`.

Ao registrar um lançamento, o `EntryRepository` adiciona o lançamento e uma mensagem `EntryCreated` ao mesmo `DbContext`.

Um único `SaveChangesAsync` persiste as duas alterações, mantendo o lançamento e a mensagem na mesma unidade de persistência.

O objetivo é evitar que a disponibilidade do RabbitMQ determine o sucesso do registro financeiro. Se o RabbitMQ ou o Worker estiverem indisponíveis, o lançamento continua sendo salvo e a mensagem permanece pendente na Outbox.

A migration foi criada com:

```powershell
dotnet ef migrations add AddOutboxMessages `
    --project CashFlow.Infrastructure `
    --startup-project CashFlow.Api
```

E aplicada com:

```powershell
dotnet ef database update `
    --project CashFlow.Infrastructure `
    --startup-project CashFlow.Api
```

## Evento EntryCreated

Foi criado o contrato `EntryCreatedEvent` para representar a mensagem enviada após o registro de um lançamento.

O evento contém:

```text
EntryId
Type
Amount
Description
OccurredAt
```

O mesmo contrato é utilizado na serialização da Outbox e na desserialização realizada pelo consumidor.

## Publicação da Outbox

O `OutboxPublisherService`, executado pelo Worker, consulta mensagens cujo `ProcessedAt` ainda não foi preenchido.

As mensagens são publicadas no RabbitMQ utilizando:

```text
Exchange: cashflow.entries
Routing key: entry.created
Queue: cashflow.daily-balance
```

Após a publicação, a mensagem da Outbox é marcada como processada.

A validação confirmou a alteração de:

```text
ProcessedAt = NULL
```

para uma data de processamento após a publicação.

## Consumo e atualização do consolidado

O `DailyBalanceConsumerService` mantém um consumidor conectado à fila:

```text
cashflow.daily-balance
```

O consumo utiliza confirmação manual (`ACK`). A mensagem somente é confirmada após o processamento realizado pelo consumidor. Em caso de erro, é utilizado `NACK` com reenvio para a fila.

Ao receber um `EntryCreatedEvent`, o consumidor:

1. verifica se o `EntryId` já foi processado;
2. identifica o dia do lançamento em UTC;
3. localiza ou cria o registro correspondente em `daily_balances`;
4. soma o valor em créditos ou débitos conforme o tipo do lançamento;
5. registra o `EntryId` em `processed_messages`;
6. persiste as alterações;
7. confirma a mensagem no RabbitMQ.

A confirmação somente ocorre depois da persistência do consolidado.

## Persistência do consolidado

Foi criada a estrutura `DailyBalanceRecord` para armazenar o consolidado de cada dia.

A tabela `daily_balances` possui:

```text
Date          date           PK
TotalCredits  decimal(18,2)
TotalDebits   decimal(18,2)
```

A migration foi criada com:

```powershell
dotnet ef migrations add AddDailyBalances `
    --project CashFlow.Infrastructure `
    --startup-project CashFlow.Api
```

E aplicada com:

```powershell
dotnet ef database update `
    --project CashFlow.Infrastructure `
    --startup-project CashFlow.Api
```

A estrutura foi validada diretamente no MySQL:

```powershell
docker exec cashflow-mysql mysql -uroot -proot cashflow -e "DESCRIBE daily_balances;"
```

## Validação do processamento assíncrono

A API foi executada inicialmente sem o Worker.

Foi registrado um crédito de R$ 400,00 e o lançamento foi salvo normalmente, enquanto `daily_balances` permaneceu vazia. Isso confirmou que o registro financeiro não depende do processamento do consolidado.

Após iniciar o Worker, a mensagem pendente foi publicada e consumida. O resultado foi:

```text
Date          TotalCredits  TotalDebits
2026-09-02    400.00        0.00
```

A fila também foi consultada:

```powershell
docker exec cashflow-rabbitmq rabbitmqctl list_queues name messages consumers
```

Resultado após o processamento:

```text
cashflow.daily-balance  messages: 0  consumers: 1
```

Em seguida foi registrado um débito de R$ 150,00 para o mesmo dia. O consolidado passou a apresentar:

```text
Date          TotalCredits  TotalDebits
2026-09-02    400.00        150.00
```

## Idempotência

Como o RabbitMQ pode entregar uma mensagem novamente, o consumidor precisa impedir que o mesmo lançamento seja somado mais de uma vez.

Foi criada a tabela `processed_messages`, utilizando o `EntryId` como chave primária:

```text
EntryId       char(36)       PK
ProcessedAt   datetime(6)
```

A migration foi criada com:

```powershell
dotnet ef migrations add AddProcessedMessages `
    --project CashFlow.Infrastructure `
    --startup-project CashFlow.Api
```

E aplicada com:

```powershell
dotnet ef database update `
    --project CashFlow.Infrastructure `
    --startup-project CashFlow.Api
```

A estrutura foi validada com:

```powershell
docker exec cashflow-mysql mysql -uroot -proot cashflow -e "DESCRIBE processed_messages;"
```

Antes de alterar o consolidado, o consumidor verifica se o `EntryId` já existe em `processed_messages`.

Se já existir, nenhuma soma é realizada. Se ainda não existir, a atualização de `daily_balances` e a inclusão em `processed_messages` são realizadas no mesmo `SaveChangesAsync`.

## Validação da idempotência

Foi criado um novo crédito de R$ 50,00. Após o primeiro processamento, o estado ficou:

```text
TotalCredits: 450.00
TotalDebits:  150.00
```

O lançamento também foi registrado em `processed_messages`:

```text
EntryId: be0ce604-2c22-4455-8787-edf619e8911a
```

Para simular uma nova entrega, a mensagem correspondente da Outbox foi marcada novamente como pendente, definindo seu `ProcessedAt` como `NULL`.

O Worker republicou o mesmo `EntryCreatedEvent`. A Outbox recebeu uma nova data em `ProcessedAt`, confirmando a republicação.

Mesmo após a segunda entrega, o consolidado permaneceu:

```text
TotalCredits: 450.00
TotalDebits:  150.00
```

Portanto, o crédito de R$ 50,00 não foi somado novamente, validando o comportamento idempotente do consumidor.

## Validação do projeto

Após as alterações foram executados:

```powershell
dotnet build
dotnet test
```

Resultado:

```text
Build: sucesso
Testes: 19
Aprovados: 19
Falhas: 0
```

## Estado atual

O fluxo assíncrono está funcional de ponta a ponta:

```text
POST /api/entries
        ↓
entries + outbox_messages
        ↓
OutboxPublisherService
        ↓
RabbitMQ
        ↓
DailyBalanceConsumerService
        ↓
daily_balances + processed_messages
        ↓
ACK
```

Foram validados o processamento de crédito, o processamento de débito, a independência da API em relação ao Worker e o reprocessamento idempotente de uma mensagem duplicada.

## Estado final

A consulta do consolidado diário utiliza diretamente a tabela `daily_balances`, evitando recalcular o resultado a partir dos lançamentos em cada requisição.

O endpoint também foi submetido aos testes locais de carga documentados em `docs/04-consolidacao-diaria.md`.

Com isso, o fluxo principal proposto para o desafio está concluído.