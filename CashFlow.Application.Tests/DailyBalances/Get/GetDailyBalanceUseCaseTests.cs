using CashFlow.Application.DailyBalances.Get;
using CashFlow.Application.Tests.Fakes;
using CashFlow.Domain.Entities;

namespace CashFlow.Application.Tests.DailyBalances.Get;

public sealed class GetDailyBalanceUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_DeveRetornarConsolidadoQuandoExistir()
    {
        // Preparação
        var date = new DateOnly(2026, 9, 2);

        var repository = new DailyBalanceRepositoryFake
        {
            Balance = new DailyBalance(date, 500m, 150m)
        };

        var useCase = new GetDailyBalanceUseCase(repository);

        // Execução
        var result = await useCase.ExecuteAsync(date);

        // Validação
        Assert.Equal(date, result.Date);
        Assert.Equal(500m, result.TotalCredits);
        Assert.Equal(150m, result.TotalDebits);
        Assert.Equal(350m, result.Balance);
    }

    [Fact]
    public async Task ExecuteAsync_DeveRetornarConsolidadoZeradoQuandoNaoExistir()
    {
        // Preparação
        var date = new DateOnly(2026, 9, 3);
        var repository = new DailyBalanceRepositoryFake();
        var useCase = new GetDailyBalanceUseCase(repository);

        // Execução
        var result = await useCase.ExecuteAsync(date);

        // Validação
        Assert.Equal(date, result.Date);
        Assert.Equal(0m, result.TotalCredits);
        Assert.Equal(0m, result.TotalDebits);
        Assert.Equal(0m, result.Balance);
    }

    [Fact]
    public async Task ExecuteAsync_DeveEncaminharDataECancellationTokenAoRepositorio()
    {
        // Preparação
        var date = new DateOnly(2026, 9, 2);
        var cancellationToken = new CancellationTokenSource().Token;
        var repository = new DailyBalanceRepositoryFake();
        var useCase = new GetDailyBalanceUseCase(repository);

        // Execução
        await useCase.ExecuteAsync(date, cancellationToken);

        // Validação
        Assert.Equal(date, repository.DateReceived);
        Assert.Equal(cancellationToken, repository.CancellationTokenReceived);
    }
}