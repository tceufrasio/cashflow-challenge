using CashFlow.Application.Contracts.Persistence;
using CashFlow.Domain.Entities;
using CashFlow.Domain.Enums;

namespace CashFlow.Application.DailyBalances.Get;

/// <summary>
/// Calcula o consolidado financeiro de um determinado dia.
/// </summary>
public sealed class GetDailyBalanceUseCase
{
    private readonly IEntryRepository _repository;

    public GetDailyBalanceUseCase(IEntryRepository repository)
    {
        _repository = repository;
    }

    public async Task<DailyBalance> ExecuteAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        // Define o intervalo completo do dia em UTC.
        var start = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var end = start.AddDays(1);

        var entries = await _repository.GetByPeriodAsync(start, end, cancellationToken);

        // Os valores permanecem positivos; o tipo define crédito ou débito.
        var totalCredits = entries
            .Where(x => x.Tipo == EntryType.Credito)
            .Sum(x => x.Valor);

        var totalDebits = entries
            .Where(x => x.Tipo == EntryType.Debito)
            .Sum(x => x.Valor);

        return new DailyBalance(
            date,
            totalCredits,
            totalDebits);
    }
}