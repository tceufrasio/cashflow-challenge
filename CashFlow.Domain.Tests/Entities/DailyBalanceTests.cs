using CashFlow.Domain.Entities;

namespace CashFlow.Domain.Tests.Entities;

public sealed class DailyBalanceTests
{
    [Fact]
    public void Constructor_ComValoresValidos_DeveCriarConsolidado()
    {
        // Preparação
        var data = new DateOnly(2026, 9, 2);

        // Execução
        var consolidado = new DailyBalance(data, 1500m, 300m);

        // Validação
        Assert.Equal(data, consolidado.Date);
        Assert.Equal(1500m, consolidado.TotalCredits);
        Assert.Equal(300m, consolidado.TotalDebits);
        Assert.Equal(1200m, consolidado.Balance);
    }

    [Fact]
    public void Constructor_ComSaldoNegativo_DeveCalcularCorretamente()
    {
        // Preparação
        var data = new DateOnly(2026, 9, 2);

        // Execução
        var consolidado = new DailyBalance(data, 500m, 800m);

        // Validação
        Assert.Equal(-300m, consolidado.Balance);
    }

    [Fact]
    public void Constructor_ComCreditoNegativo_DeveLancarExcecao()
    {
        // Preparação
        var data = new DateOnly(2026, 9, 2);

        // Execução
        var excecao = Assert.Throws<ArgumentOutOfRangeException>(() => new DailyBalance(data, -1m, 100m));

        // Validação
        Assert.Contains("créditos não pode ser negativo", excecao.Message);
    }

    [Fact]
    public void Constructor_ComDebitoNegativo_DeveLancarExcecao()
    {
        // Preparação
        var data = new DateOnly(2026, 9, 2);

        // Execução
        var excecao = Assert.Throws<ArgumentOutOfRangeException>(() => new DailyBalance(data, 100m, -1m));

        // Validação
        Assert.Contains("débitos não pode ser negativo", excecao.Message);
    }
}