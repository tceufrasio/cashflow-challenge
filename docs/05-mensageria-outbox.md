# Mensageria e Transactional Outbox

## Objetivo

A mensageria foi adicionada para desacoplar o registro dos lançamentos do processamento do consolidado diário.

O lançamento financeiro deve continuar sendo registrado mesmo quando o RabbitMQ ou o processamento do consolidado estiverem indisponíveis.

O fluxo implementado nesta etapa é:

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

Um único `SaveChangesAsync` persiste as duas alterações.

O objetivo é evitar que a disponibilidade do RabbitMQ determine o sucesso do registro financeiro.

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

## Publicação da Outbox

O `OutboxPublisherService`, executado pelo Worker, consulta mensagens cujo `ProcessedAt` ainda não foi preenchido.

As mensagens são publicadas no RabbitMQ utilizando:

```text
Exchange: cashflow.entries
Routing key: entry.created
Queue: cashflow.daily-balance
```

Após a publicação, a mensagem da Outbox é marcada como processada.

A validação realizada confirmou a alteração de:

```text
ProcessedAt = NULL
```

para uma data de processamento após a publicação.

## Consumo

O `DailyBalanceConsumerService` mantém um consumidor conectado à fila:

```text
cashflow.daily-balance
```

O consumo utiliza confirmação manual (`ACK`).

A mensagem somente é confirmada após o processamento realizado pelo consumidor. Em caso de erro, é utilizado `NACK` com reenvio para a fila.

Nesta etapa o consumidor apenas recebe a mensagem e registra seu conteúdo. A atualização efetiva do consolidado será implementada na próxima etapa.

## Validação do RabbitMQ

Antes da inicialização do consumidor:

```text
messages: 1
consumers: 0
```

Após iniciar o consumidor:

```text
messages: 0
consumers: 1
```

A consulta utilizada foi:

```powershell
docker exec cashflow-rabbitmq rabbitmqctl list_queues name messages consumers
```

Isso confirmou o fluxo:

```text
Outbox
  ↓
Publisher
  ↓
RabbitMQ
  ↓
Queue
  ↓
Consumer
  ↓
ACK
```

## Persistência do consolidado

Foi criada a estrutura `DailyBalanceRecord` para armazenar o resultado consolidado de cada dia.

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

## Validação do projeto

Após as alterações:

```powershell
dotnet build
dotnet test
```

Resultado:

```text
Build: sucesso
Testes: 19
Falhas: 0
```

## Próxima etapa

O próximo passo é fazer o `DailyBalanceConsumerService` processar o evento `EntryCreated` e atualizar a tabela `daily_balances`.

Depois serão tratados os cenários de reprocessamento e idempotência para evitar que uma mensagem entregue novamente altere o consolidado mais de uma vez.