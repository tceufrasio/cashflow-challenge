using CashFlow.Application.Entries.Create;
using CashFlow.Application.Tests.Fakes;
using CashFlow.Domain.Enums;

namespace CashFlow.Application.Tests.Entries.Create;

public class CreateEntryUseCaseTests
{
    // Verifica se um lançamento válido é persistido e retornado corretamente.
    [Fact]
    public async Task ExecuteAsync_WithValidRequest_ShouldPersistAndReturnEntry()
    {
        // Preparação
        var repository = new EntryRepositoryFake();
        var useCase = new CreateEntryUseCase(repository);
        var dataOcorrencia = DateTimeOffset.UtcNow;
        var request = new CreateEntryRequest(EntryType.Credito, 150.50m, "Venda realizada", dataOcorrencia);

        // Execução
        var result = await useCase.ExecuteAsync(request);

        // Validação
        Assert.NotNull(repository.EntryAdded);
        Assert.Equal(result.Id, repository.EntryAdded.Id);
        Assert.Equal(request.Type, result.Type);
        Assert.Equal(request.Amount, result.Amount);
        Assert.Equal(request.Description, result.Description);
        Assert.Equal(request.OccurredAt, result.OccurredAt);
    }

    // Verifica se dados inválidos são rejeitados antes da persistência.
    [Fact]
    public async Task ExecuteAsync_WithInvalidAmount_ShouldNotPersistEntry()
    {
        // Preparação
        var repository = new EntryRepositoryFake();
        var useCase = new CreateEntryUseCase(repository);
        var request = new CreateEntryRequest(EntryType.Credito, 0, "Venda realizada", DateTimeOffset.UtcNow);

        // Execução
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => useCase.ExecuteAsync(request));

        // Validação
        Assert.Null(repository.EntryAdded);
    }

    // Verifica se o token de cancelamento é encaminhado para a persistência.
    [Fact]
    public async Task ExecuteAsync_ShouldForwardCancellationToken()
    {
        // Preparação
        var repository = new EntryRepositoryFake();
        var useCase = new CreateEntryUseCase(repository);
        var request = new CreateEntryRequest(EntryType.Credito, 100m, "Venda realizada", DateTimeOffset.UtcNow);
        using var cancellationTokenSource = new CancellationTokenSource();

        // Execução
        await useCase.ExecuteAsync(request, cancellationTokenSource.Token);

        // Validação
        Assert.Equal(cancellationTokenSource.Token, repository.CancellationTokenReceived);
    }
}