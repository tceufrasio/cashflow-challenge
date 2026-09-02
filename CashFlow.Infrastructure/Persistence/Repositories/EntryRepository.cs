using CashFlow.Application.Contracts.Persistence;
using CashFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementa a persistência dos lançamentos financeiros.
/// </summary>
public sealed class EntryRepository : IEntryRepository
{
    private readonly CashFlowDbContext _context;

    public EntryRepository(CashFlowDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Entry entry, CancellationToken cancellationToken = default)
    {
        await _context.Entries.AddAsync(entry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Entry>> GetByPeriodAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
    {
        // A consulta é somente para leitura e considera o início inclusivo e o fim exclusivo.
        return await _context.Entries
            .AsNoTracking()
            .Where(x =>
                x.DataOcorrencia >= start &&
                x.DataOcorrencia < end)
            .ToListAsync(cancellationToken);
    }
}