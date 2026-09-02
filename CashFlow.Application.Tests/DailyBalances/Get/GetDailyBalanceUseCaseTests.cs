using CashFlow.Application.DailyBalances.Get;
using CashFlow.Application.Tests.Fakes;
using CashFlow.Domain.Entities;
using CashFlow.Domain.Enums;
 
namespace CashFlow.Application.Tests.DailyBalances.Get;

public sealed class GetDailyBalanceUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ComLancamentosDoDia_DeveCalcularConsolidado()
    {
        // Preparação
        var repository = new EntryRepositoryFake();
        repository.Entries.Add(new Entry(EntryType.Credito, 1000m, "Venda", new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero)));
        repository.Entries.Add(new Entry(EntryType.Credito, 500m, "Recebimento", new DateTimeOffset(2026, 9, 2, 14, 0, 0, TimeSpan.Zero)));
        repository.Entries.Add(new Entry(EntryType.Debito, 300m, "Pagamento", new DateTimeOffset(2026, 9, 2, 16, 0, 0, TimeSpan.Zero)));

        var useCase = new GetDailyBalanceUseCase(repository);

        // Execução
        var result = await useCase.ExecuteAsync(new DateOnly(2026, 9, 2));

        // Validação
        Assert.Equal(1500m, result.TotalCredits);
        Assert.Equal(300m, result.TotalDebits);
        Assert.Equal(1200m, result.Balance);
    }

    [Fact]
    public async Task ExecuteAsync_ComLancamentoDeOutroDia_DeveIgnorarLancamento()
    {
        // Preparação
        var repository = new EntryRepositoryFake();
        repository.Entries.Add(new Entry(EntryType.Credito, 1000m, "Venda do dia", new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero)));
        repository.Entries.Add(new Entry(EntryType.Credito, 500m, "Venda do dia seguinte", new DateTimeOffset(2026, 9, 3, 10, 0, 0, TimeSpan.Zero)));

        var useCase = new GetDailyBalanceUseCase(repository);

        // Execução
        var result = await useCase.ExecuteAsync(new DateOnly(2026, 9, 2));

        // Validação
        Assert.Equal(1000m, result.TotalCredits);
        Assert.Equal(0m, result.TotalDebits);
        Assert.Equal(1000m, result.Balance);
    }

    [Fact]
    public async Task ExecuteAsync_SemLancamentos_DeveRetornarConsolidadoZerado()
    {
        // Preparação
        var repository = new EntryRepositoryFake();
        var useCase = new GetDailyBalanceUseCase(repository);

        // Execução
        var result = await useCase.ExecuteAsync(new DateOnly(2026, 9, 2));

        // Validação
        Assert.Equal(0m, result.TotalCredits);
        Assert.Equal(0m, result.TotalDebits);
        Assert.Equal(0m, result.Balance);
    }
}