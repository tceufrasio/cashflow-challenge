# 03 - API de Lançamentos

## Objetivo

Expor o registro de lançamentos financeiros através de uma API HTTP, utilizando o caso de uso já implementado na camada Application.

## Endpoint

Foi criado o endpoint:

```http
POST /api/entries
```

O fluxo da operação é:

```text
HTTP
 ↓
EntriesController
 ↓
CreateEntryUseCase
 ↓
Entry
 ↓
IEntryRepository
 ↓
EntryRepository
 ↓
MySQL
```

## Contrato HTTP

A API possui um contrato próprio para entrada dos dados:

```text
CreateEntryRequest
├── Type
├── Amount
├── Description
└── OccurredAt
```

O contrato HTTP é separado do contrato da Application para evitar acoplamento entre a interface externa e os casos de uso.

## Injeção de dependência

A camada Application passou a registrar seus próprios serviços através de `AddApplication()`.

O `Program.cs` atua como ponto de composição da aplicação:

```text
AddApplication()
AddInfrastructure(...)
```

## Tratamento de erros

Foi implementado tratamento centralizado de exceções utilizando `IExceptionHandler` e `ProblemDetails`.

Violações das regras atuais do domínio, como valor inválido, descrição obrigatória ou tipo inexistente, são retornadas como:

```text
400 Bad Request
```

Isso evita tratamento repetido de exceções dentro dos controllers.

## Validação

O fluxo foi validado manualmente com a API e o MySQL executando localmente.

Foram verificados:

- lançamento válido retornando `201 Created`;
- persistência do lançamento no MySQL;
- valor igual a zero retornando `400`;
- descrição vazia retornando `400`;
- tipo de lançamento inválido retornando `400`;
- build da solução sem erros;
- 12 testes automatizados passando.

## Próximo passo

Implementar a consulta e consolidação diária dos lançamentos, mantendo essa responsabilidade separada do registro das movimentações.