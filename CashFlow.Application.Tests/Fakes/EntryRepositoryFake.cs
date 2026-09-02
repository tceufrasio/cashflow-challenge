using CashFlow.Application.Contracts.Persistence;
using CashFlow.Domain.Entities;

namespace CashFlow.Application.Tests.Fakes;

/// <summary>
/// Implementação em memória do repositório utilizada nos testes.
/// </summary>
public sealed class EntryRepositoryFake : IEntryRepository
{
    // Lançamento recebido pelo método de inclusão.
    public Entry? EntryAdded { get; private set; }

    // Token recebido pelo repositório para validar sua propagação.
    public CancellationToken CancellationTokenReceived { get; private set; }

    // Lançamentos disponíveis para as consultas realizadas nos testes.
    public List<Entry> Entries { get; } = [];

    public Task AddAsync(Entry entry, CancellationToken cancellationToken = default)
    {
        EntryAdded = entry;
        CancellationTokenReceived = cancellationToken;
        Entries.Add(entry);

        return Task.CompletedTask;
    }
}