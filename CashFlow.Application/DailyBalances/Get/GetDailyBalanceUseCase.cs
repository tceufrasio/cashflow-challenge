using CashFlow.Application.Contracts.Persistence;
using CashFlow.Domain.Entities;

namespace CashFlow.Application.DailyBalances.Get;

/// <summary>
/// Retorna o consolidado financeiro de um determinado dia.
/// </summary>
public sealed class GetDailyBalanceUseCase
{
    private readonly IDailyBalanceRepository _repository;

    public GetDailyBalanceUseCase(IDailyBalanceRepository repository)
    {
        _repository = repository;
    }

    public async Task<DailyBalance> ExecuteAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        // Consulta o consolidado previamente processado pelo Worker.
        var balance = await _repository.GetByDateAsync(date, cancellationToken);

        // Uma data ainda sem movimentações possui consolidado zerado.
        return balance ?? new DailyBalance(date, 0, 0);
    }
}