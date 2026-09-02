using CashFlow.Application.Contracts.Persistence;
using CashFlow.Domain.Entities;

namespace CashFlow.Application.Entries.Create;

/// <summary>
/// Registra um novo lançamento financeiro.
/// </summary>
public sealed class CreateEntryUseCase
{
    private readonly IEntryRepository _repository;

    public CreateEntryUseCase(IEntryRepository repository)
    {
        _repository = repository;
    } 

    public async Task<CreateEntryResult> ExecuteAsync(CreateEntryRequest request, CancellationToken cancellationToken = default)
    {
        // A entidade concentra as regras de negócio do lançamento. 
        var lancamento = new Entry(request.Type, request.Amount, request.Description, request.OccurredAt);

        await _repository.AddAsync(lancamento, cancellationToken);

        return new CreateEntryResult(lancamento.Id, lancamento.Tipo, lancamento.Valor, lancamento.Descricao, lancamento.DataOcorrencia);
    }
}