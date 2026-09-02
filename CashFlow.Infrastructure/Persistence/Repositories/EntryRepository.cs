using System.Text.Json;
using CashFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using CashFlow.Infrastructure.Messaging.Outbox;
using CashFlow.Application.Contracts.Persistence;
using CashFlow.Application.Contracts.Messaging;

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
        // Registra o lançamento e o evento na mesma unidade de persistência.
        await _context.Entries.AddAsync(entry, cancellationToken);

        var payload = JsonSerializer.Serialize(new EntryCreatedEvent(entry.Id, entry.Tipo, entry.Valor, entry.Descricao, entry.DataOcorrencia));

        var message = new OutboxMessage("EntryCreated", payload);

        await _context.OutboxMessages.AddAsync(message, cancellationToken);

        // Um único SaveChanges mantém lançamento e mensagem na mesma transação.
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