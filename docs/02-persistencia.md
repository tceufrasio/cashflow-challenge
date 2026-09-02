# 02 - Persistência

## Objetivo

Adicionar persistência aos lançamentos financeiros, mantendo a camada de aplicação desacoplada do banco de dados e da tecnologia de acesso a dados.

## Banco de dados

Foi utilizado MySQL 8.4.

Os lançamentos possuem estrutura bem definida e exigem consistência transacional. Um banco relacional atende bem esse cenário e também às consultas e agregações necessárias para a consolidação diária.

PostgreSQL também atenderia aos requisitos, porém não havia necessidade técnica que justificasse a troca. O MySQL foi mantido pela simplicidade e familiaridade operacional.

## Entity Framework Core

O acesso aos dados foi implementado com Entity Framework Core e o provider Pomelo para MySQL.

Pacotes utilizados:

- `Microsoft.EntityFrameworkCore` 9.0.0
- `Microsoft.EntityFrameworkCore.Design` 9.0.0
- `Pomelo.EntityFrameworkCore.MySql` 9.0.0

As versões foram mantidas alinhadas para evitar conflitos entre as dependências do EF Core.

## Estrutura da persistência

A implementação está concentrada em `CashFlow.Infrastructure`:

```text
Persistence
├── Configurations
│   └── EntryConfiguration.cs
├── Migrations
├── Repositories
│   └── EntryRepository.cs
└── CashFlowDbContext.cs
```

### CashFlowDbContext

`CashFlowDbContext` representa a sessão do EF Core com o banco e expõe os lançamentos através de `DbSet<Entry>`.

Os mapeamentos são carregados a partir da própria Infrastructure:

```csharp
modelBuilder.ApplyConfigurationsFromAssembly(typeof(CashFlowDbContext).Assembly);
```

Isso permite manter as configurações de persistência fora da entidade de domínio.

### EntryConfiguration

O mapeamento de `Entry` define:

- tabela `entries`;
- `Id` como chave primária;
- `Tipo` como inteiro;
- `Valor` como `decimal(18,2)`;
- `Descricao` limitada a 200 caracteres;
- datas obrigatórias;
- índice em `DataOcorrencia`.

O índice foi criado porque a consolidação diária utilizará consultas dos lançamentos por período.

### EntryRepository

`EntryRepository` implementa o contrato `IEntryRepository`, definido na camada Application.

O fluxo de dependência permanece:

```text
Application
    └── IEntryRepository
              ↑
Infrastructure
    └── EntryRepository
              ↓
       CashFlowDbContext
              ↓
            MySQL
```

Dessa forma, os casos de uso não possuem dependência direta de EF Core ou MySQL.

## Injeção de dependência

A Infrastructure possui um método de extensão responsável por registrar seus componentes:

```csharp
services.AddDbContext<CashFlowDbContext>(...);
services.AddScoped<IEntryRepository, EntryRepository>();
```

A API atua como Composition Root, fornecendo a configuração necessária para inicializar a Infrastructure.

## Docker

O ambiente local utiliza MySQL 8.4 através de Docker Compose.

O `docker-compose.yml` configura:

- container `cashflow-mysql`;
- banco `cashflow`;
- volume persistente;
- health check;
- porta 3306.

O banco pode ser iniciado com:

```powershell
docker compose up -d
```

## Migrations

O schema é versionado através das migrations do Entity Framework Core.

A migration inicial foi criada com:

```powershell
dotnet ef migrations add InitialCreate --project CashFlow.Infrastructure --startup-project CashFlow.Api --output-dir Persistence/Migrations
```

Após revisar o schema gerado, a migration foi aplicada:

```powershell
dotnet ef database update --project CashFlow.Infrastructure --startup-project CashFlow.Api
```

A migration cria a tabela `entries` e o índice de `DataOcorrencia`.

## Schema inicial

```text
entries
├── Id              char(36)       PK
├── Tipo            int
├── Valor           decimal(18,2)
├── Descricao       varchar(200)
├── DataOcorrencia  datetime(6)    INDEX
└── CriadoEm        datetime(6)
```

Também é criada a tabela `__EFMigrationsHistory`, utilizada pelo EF Core para controlar as migrations aplicadas.

## Decisões técnicas

### Domínio independente da persistência

A entidade `Entry` não possui dependência de EF Core ou MySQL. O mapeamento fica exclusivamente na Infrastructure.

### Valores monetários

Valores financeiros utilizam `decimal(18,2)`, evitando tipos de ponto flutuante para representação monetária.

### Índice por data

`DataOcorrencia` possui índice para favorecer as consultas por período necessárias à consolidação diária.

### Repository

A interface pertence à Application e a implementação à Infrastructure, mantendo o caso de uso desacoplado da tecnologia de persistência.

### Migrations

Alterações no schema são versionadas e revisadas antes de serem aplicadas ao banco.

## Validação

A etapa foi validada com:

- build da solução sem erros ou warnings;
- MySQL executando em Docker com status `healthy`;
- criação do banco `cashflow`;
- aplicação da migration `InitialCreate`;
- validação da tabela `entries`;
- validação da chave primária;
- validação do índice `IX_entries_DataOcorrencia`.

## Próximo passo

Expor o caso de uso de criação de lançamento através da API e validar o fluxo completo:

```text
HTTP → API → Application → Infrastructure → MySQL
```