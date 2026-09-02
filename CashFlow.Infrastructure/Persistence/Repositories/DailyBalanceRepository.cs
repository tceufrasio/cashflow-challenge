using CashFlow.Application.Contracts.Persistence;
using CashFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementa a leitura do consolidado diário.
/// </summary>
public sealed class DailyBalanceRepository : IDailyBalanceRepository
{
    private readonly CashFlowDbContext _context;

    public DailyBalanceRepository(CashFlowDbContext context)
    {
        _context = context;
    }

    public async Task<DailyBalance?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        // Consulta diretamente o consolidado previamente processado pelo Worker.
        var balance = await _context.DailyBalances.AsNoTracking().SingleOrDefaultAsync(x => x.Date == date, cancellationToken);

        if (balance is null)
            return null;

        return new DailyBalance(balance.Date, balance.TotalCredits, balance.TotalDebits);
    }
}