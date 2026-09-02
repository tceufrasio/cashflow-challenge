# Consolidação diária

## Objetivo

Disponibilizar a consulta do saldo consolidado de um determinado dia,
considerando os lançamentos de crédito e débito registrados no fluxo de
caixa.

O consolidado apresenta:

-   total de créditos;
-   total de débitos;
-   saldo do dia.

O saldo é calculado pela diferença entre créditos e débitos:

``` text
Saldo = Total de créditos - Total de débitos
```

A consolidação é processada de forma assíncrona pelo Worker e armazenada
previamente no banco. Dessa forma, a consulta não precisa percorrer os
lançamentos a cada requisição.

## Domínio

A entidade `DailyBalance` representa o consolidado financeiro de um dia.

Ela utiliza `DateOnly`, pois o consolidado representa uma data e não um
instante específico.

O saldo não é armazenado separadamente. Ele é calculado a partir dos
totais:

``` text
Balance = TotalCredits - TotalDebits
```

Os totais de créditos e débitos não podem ser negativos.

## Processamento do consolidado

O processamento dos lançamentos ocorre de forma assíncrona:

``` text
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
daily_balances
```

O `DailyBalanceConsumerService` recebe o evento `EntryCreated` e
atualiza os totais correspondentes à data do lançamento.

A data do evento é interpretada em UTC para determinar o dia do
consolidado.

O processamento assíncrono, a Transactional Outbox e a idempotência
estão detalhados em `docs/05-mensageria-outbox.md`.

## Application

O contrato `IDailyBalanceRepository` define a leitura do consolidado
diário:

``` text
GetByDateAsync(DateOnly date)
```

O `GetDailyBalanceUseCase` depende desse contrato e não conhece EF Core
ou MySQL.

O fluxo da consulta é:

``` text
Data solicitada
      ↓
GetDailyBalanceUseCase
      ↓
IDailyBalanceRepository
      ↓
daily_balances
      ↓
DailyBalance
```

Quando não existe um consolidado para a data solicitada, o caso de uso
retorna um `DailyBalance` zerado.

## Persistência

O `DailyBalanceRepository` implementa `IDailyBalanceRepository`
utilizando EF Core.

A consulta é realizada diretamente na tabela `daily_balances`.

Como o registro é utilizado somente para leitura, a consulta utiliza:

``` csharp
.AsNoTracking()
```

A tabela mantém um registro por data:

``` text
Date          date           PK
TotalCredits  decimal(18,2)
TotalDebits   decimal(18,2)
```

O saldo não precisa ser persistido, pois continua sendo derivado dos
dois totais.

Essa abordagem evita recalcular o consolidado consultando todos os
lançamentos do dia em cada requisição.

## API

O endpoint de consulta é:

``` http
GET /api/daily-balances/{date}
```

Exemplo:

``` http
GET /api/daily-balances/2026-09-02
```

A API recebe a data, chama o `GetDailyBalanceUseCase` e retorna o
consolidado previamente processado.

Para uma data sem movimentações, os valores retornados são zero.

## Testes automatizados

Os testes do `GetDailyBalanceUseCase` validam:

-   retorno do consolidado quando existe;
-   retorno zerado quando a data ainda não possui consolidado;
-   encaminhamento da data e do `CancellationToken` ao repositório.

Também permanecem os testes de domínio para validar as regras de
`DailyBalance`.

A validação foi executada com:

``` powershell
dotnet build
dotnet test
```

Resultado:

``` text
Build: sucesso
Total de testes: 19
Bem-sucedidos: 19
Falharam: 0
```

## Validação manual

A API foi executada localmente:

``` powershell
dotnet run --project CashFlow.Api
```

Durante a validação, a aplicação iniciou em `http://localhost:5192`.

A consulta do consolidado existente foi realizada com:

``` powershell
Invoke-RestMethod `
    -Uri "http://localhost:5192/api/daily-balances/2026-09-02" `
    -Method Get
```

Resultado:

``` text
date          : 2026-09-02
totalCredits  : 450,00
totalDebits   : 150,00
balance       : 300,00
```

Também foi consultada uma data sem movimentações:

``` powershell
Invoke-RestMethod `
    -Uri "http://localhost:5192/api/daily-balances/2026-09-10" `
    -Method Get
```

Resultado:

``` text
date          : 2026-09-10
totalCredits  : 0
totalDebits   : 0
balance       : 0
```

Com isso foi validado o fluxo atual de leitura:

``` text
HTTP
 ↓
API
 ↓
Application
 ↓
IDailyBalanceRepository
 ↓
Infrastructure
 ↓
EF Core
 ↓
daily_balances
```

## Decisão de arquitetura

A primeira versão do consolidado calculava créditos e débitos
consultando os lançamentos do dia durante cada GET.

A implementação atual transfere esse cálculo para o processamento
assíncrono e mantém o resultado em `daily_balances`.

Isso reduz o trabalho realizado pelo endpoint de consulta e prepara o
consolidado para o requisito de pico de 50 consultas por segundo.

O atendimento desse volume ainda deve ser validado por teste de
desempenho; a arquitetura por si só não é tratada como comprovação de
capacidade.

## Teste de carga

Foi realizado um teste local para validar o comportamento do endpoint de consolidação sob consultas concorrentes.

O primeiro cenário executou uma rajada de 50 requisições concorrentes para:

```http
GET /api/daily-balances/2026-09-02
```

Resultado:

```text
Requisições: 50
Sucessos: 50
Falhas: 0
Percentual de falhas: 0%
Tempo da execução: 0,661 s
```

Também foi realizado um teste adicional de estabilidade com 10 rajadas de 50 requisições.

Resultado:

```text
Requisições: 500
Sucessos: 500
Falhas: 0
Percentual de falhas: 0%
```

Os testes foram executados localmente utilizando PowerShell.

O tempo total do segundo teste não foi utilizado para calcular throughput, pois inclui o custo de criação dos jobs do PowerShell e intervalos adicionados entre as rodadas.

Os resultados demonstram que, no ambiente local e no cenário testado, o endpoint respondeu às rajadas de 50 consultas concorrentes sem perda de requisições.

Esse teste não representa um benchmark de ambiente produtivo.

## Próxima etapa

Finalizar a documentação de execução do projeto e revisar os pontos de resiliência e configuração antes da entrega.
