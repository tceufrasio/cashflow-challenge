namespace CashFlow.Infrastructure.Persistence.Models;

/// <summary>
/// Representa o consolidado diário persistido.
/// </summary>
public sealed class DailyBalanceRecord
{
    public DateOnly Date { get; private set; }
    public decimal TotalCredits { get; private set; }
    public decimal TotalDebits { get; private set; }

    private DailyBalanceRecord() { }

    public DailyBalanceRecord(DateOnly date)
    {
        Date = date;
    }

    public void AddCredit(decimal amount)
    {
        TotalCredits += amount;
    }

    public void AddDebit(decimal amount)
    {
        TotalDebits += amount;
    }
}