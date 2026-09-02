using CashFlow.Domain.Entities;

namespace CashFlow.Application.Contracts.Persistence;

/// <summary>
/// Define as operações de persistência necessárias para os lançamentos.
/// </summary>
public interface IEntryRepository
{
    // Persiste um novo lançamento financeiro.
    Task AddAsync(Entry entry, CancellationToken cancellationToken = default);

    // Retorna os lançamentos ocorridos dentro do período informado.
    Task<IReadOnlyCollection<Entry>> GetByPeriodAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default);
}