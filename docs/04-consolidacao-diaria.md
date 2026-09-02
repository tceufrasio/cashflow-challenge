# Consolidação diária

## Objetivo

Implementar a consulta do saldo consolidado de um determinado dia, considerando os lançamentos de crédito e débito registrados no fluxo de caixa.

O consolidado apresenta:

- total de créditos;
- total de débitos;
- saldo do dia.

O saldo é calculado pela diferença entre créditos e débitos:

```text
Saldo = Total de créditos - Total de débitos
```

## Domínio

Foi criada a entidade `DailyBalance`, responsável por representar o consolidado financeiro de um dia.

A entidade utiliza `DateOnly`, pois o consolidado representa um dia e não um instante específico.

O saldo não é armazenado separadamente. Ele é calculado a partir dos totais de crédito e débito, evitando manter um valor que pode ser derivado.

Também foi definido que os totais de créditos e débitos não podem ser negativos.

## Application

Foi criado o caso de uso `GetDailyBalanceUseCase`.

O fluxo executado pelo caso de uso é:

```text
Data solicitada
      ↓
Define o período do dia
      ↓
Consulta os lançamentos
      ↓
Soma os créditos
      ↓
Soma os débitos
      ↓
Cria o DailyBalance
```

Nesta implementação, o período diário é considerado em UTC.

O contrato `IEntryRepository` foi ampliado com `GetByPeriodAsync`, permitindo que a Application solicite os lançamentos de um período sem depender diretamente do EF Core ou do MySQL.

## Persistência

O `EntryRepository` implementa a consulta utilizando EF Core.

A consulta utiliza:

```csharp
.AsNoTracking()
```

porque os lançamentos são utilizados somente para leitura.

O período utiliza início inclusivo e fim exclusivo:

```text
DataOcorrencia >= início
DataOcorrencia < próximo dia
```

Isso evita a necessidade de trabalhar com horários como `23:59:59.999`.

## API

Foi criado o endpoint:

```http
GET /api/daily-balances/{date}
```

Exemplo:

```http
GET /api/daily-balances/2026-09-02
```

A API recebe a data, chama o `GetDailyBalanceUseCase` e retorna o consolidado calculado.

## Testes automatizados

Foram adicionados testes para validar:

- criação do `DailyBalance`;
- cálculo de saldo positivo;
- cálculo de saldo negativo;
- rejeição de totais negativos;
- consolidação com créditos e débitos;
- exclusão de lançamentos pertencentes a outro dia;
- consolidação de um dia sem movimentações.

Os testes foram executados pelo PowerShell:

```powershell
dotnet build
dotnet test
```

Resultado da etapa:

```text
Total de testes: 19
Bem-sucedidos: 19
Falharam: 0
```

## Validação manual

A API foi executada localmente:

```powershell
dotnet run --project CashFlow.Api
```

Foram registrados lançamentos através do endpoint:

```http
POST /api/entries
```

Exemplo utilizado no PowerShell:

```powershell
$body = @{
    type = 1
    amount = 1000.00
    description = "Venda"
    occurredAt = "2026-09-02T10:00:00Z"
} | ConvertTo-Json

Invoke-RestMethod `
    -Uri "http://localhost:5192/api/entries" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body
```

A consolidação foi consultada com:

```powershell
Invoke-RestMethod `
    -Uri "http://localhost:5192/api/daily-balances/2026-09-02" `
    -Method Get
```

Resultado obtido:

```text
date          : 2026-09-02
totalCredits  : 1650,75
totalDebits   : 300,00
balance       : 1350,75
```

O valor inclui um crédito de R$ 150,75 registrado anteriormente durante a validação da API, além dos lançamentos criados nesta etapa.

Com isso foi validado o fluxo:

```text
HTTP
 ↓
API
 ↓
Application
 ↓
Domain
 ↓
Infrastructure
 ↓
EF Core
 ↓
MySQL
```

## Próxima etapa

Evoluir o processamento para atender aos requisitos de independência e resiliência da consolidação, avaliando o uso de RabbitMQ, Worker e processamento assíncrono.