using CashFlow.Domain.Enums;

namespace CashFlow.Application.Entries.Create;

/// <summary>
/// Dados necessários para registrar um lançamento financeiro.
/// </summary>
public sealed record CreateEntryRequest(EntryType Type, decimal Amount, string Description, DateTimeOffset OccurredAt); 