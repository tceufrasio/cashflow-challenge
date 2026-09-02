using CashFlow.Domain.Entities;

namespace CashFlow.Application.Contracts.Persistence;

/// <summary>
/// Define as operações de persistência necessárias para os lançamentos.
/// </summary>
public interface IEntryRepository
{
    // Persiste um novo lançamento financeiro.
    Task AddAsync(Entry entry, CancellationToken cancellationToken = default);
}