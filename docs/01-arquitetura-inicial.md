# 01 - Arquitetura Inicial

## Objetivo

A solução CashFlow será responsável pelo registro de lançamentos financeiros de crédito e débito e pela consulta do saldo consolidado diário.

A arquitetura foi separada em camadas para manter as regras de negócio desacopladas de detalhes de infraestrutura e facilitar testes e manutenção.

## Estrutura

```text
CashFlow
├── CashFlow.Api
├── CashFlow.Application
├── CashFlow.Domain
├── CashFlow.Infrastructure
└── CashFlow.Worker