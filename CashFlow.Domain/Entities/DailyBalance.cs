namespace CashFlow.Domain.Entities;

/// <summary>
/// Representa o consolidado financeiro de um dia.
/// </summary>
public sealed class DailyBalance
{
    // Identifica o dia ao qual o consolidado pertence.
    public DateOnly Date { get; }

    // Soma dos lançamentos de crédito realizados no dia.
    public decimal TotalCredits { get; }

    // Soma dos lançamentos de débito realizados no dia. 
    public decimal TotalDebits { get; }

    // O saldo é calculado para evitar armazenar um valor que pode ser derivado dos totais.
    public decimal Balance => TotalCredits - TotalDebits;

    public DailyBalance(DateOnly date, decimal totalCredits, decimal totalDebits)
    {
        // Os totais representam valores acumulados e não podem ser negativos.
        if (totalCredits < 0)
            throw new ArgumentOutOfRangeException(nameof(totalCredits), "O total de créditos não pode ser negativo.");

        if (totalDebits < 0)
            throw new ArgumentOutOfRangeException(nameof(totalDebits), "O total de débitos não pode ser negativo.");

        Date = date;
        TotalCredits = totalCredits;
        TotalDebits = totalDebits;
    }
}