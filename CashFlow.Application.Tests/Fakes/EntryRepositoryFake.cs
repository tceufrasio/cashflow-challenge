using CashFlow.Application.Contracts.Persistence;
using CashFlow.Domain.Entities;

namespace CashFlow.Application.Tests.Fakes;

internal sealed class EntryRepositoryFake : IEntryRepository
{
    public Entry? EntryAdded { get; private set; }
    public CancellationToken CancellationTokenReceived { get; private set; }

    public Task AddAsync(Entry entry, CancellationToken cancellationToken = default)
    {
        EntryAdded = entry;
        CancellationTokenReceived = cancellationToken;

        return Task.CompletedTask;
    }   
} 