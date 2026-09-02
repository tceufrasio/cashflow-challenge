using CashFlow.Domain.Entities;

namespace CashFlow.Application.Contracts.Persistence;

/// <summary>
/// Define as operações de leitura do consolidado diário.
/// </summary>
public interface IDailyBalanceRepository
{
    // Retorna o consolidado correspondente à data informada.
    Task<DailyBalance?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default);
}