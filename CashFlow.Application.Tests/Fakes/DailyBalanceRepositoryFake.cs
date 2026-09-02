using CashFlow.Application.Contracts.Persistence;
using CashFlow.Domain.Entities;

namespace CashFlow.Application.Tests.Fakes;

/// <summary>
/// Implementação em memória do repositório de consolidado utilizada nos testes.
/// </summary>
public sealed class DailyBalanceRepositoryFake : IDailyBalanceRepository
{
    public DailyBalance? Balance { get; set; }
    public DateOnly? DateReceived { get; private set; }
    public CancellationToken CancellationTokenReceived { get; private set; }

    public Task<DailyBalance?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        DateReceived = date;
        CancellationTokenReceived = cancellationToken;

        return Task.FromResult(Balance);
    }
}