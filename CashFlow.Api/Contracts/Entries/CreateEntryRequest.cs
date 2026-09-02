using CashFlow.Domain.Enums;

namespace CashFlow.Api.Contracts.Entries;

/// <summary>
/// Dados recebidos pela API para registrar um lançamento.
/// </summary>
public sealed record CreateEntryRequest(EntryType Type, decimal Amount, string Description, DateTimeOffset OccurredAt);