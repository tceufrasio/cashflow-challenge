using CashFlow.Domain.Enums;

namespace CashFlow.Application.Contracts.Messaging;

/// <summary>
/// Representa o evento gerado após a criação de um lançamento.
/// </summary>
public sealed record EntryCreatedEvent(Guid EntryId, EntryType Type, decimal Amount, string Description, DateTimeOffset OccurredAt);