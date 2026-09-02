using CashFlow.Domain.Enums;

namespace CashFlow.Application.Entries.Create;

/// <summary>
/// Representa o lançamento criado pelo caso de uso.
/// </summary>
public sealed record CreateEntryResult(Guid Id, EntryType Type, decimal Amount, string Description, DateTimeOffset OccurredAt);     