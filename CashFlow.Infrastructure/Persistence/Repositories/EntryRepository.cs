using CashFlow.Application.Contracts.Persistence;
using CashFlow.Domain.Entities;

namespace CashFlow.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementa a persistência dos lançamentos financeiros.
/// </summary>
public sealed class EntryRepository : IEntryRepository
{
    private readonly CashFlowDbContext _dbContext;

    public EntryRepository(CashFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Entry entry, CancellationToken cancellationToken = default)
    {
        await _dbContext.Entries.AddAsync(entry, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}