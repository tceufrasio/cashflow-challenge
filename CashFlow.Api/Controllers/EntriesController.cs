using CashFlow.Application.Entries.Create;
using Microsoft.AspNetCore.Mvc;

using ApiRequest = CashFlow.Api.Contracts.Entries.CreateEntryRequest;
using ApplicationRequest = CashFlow.Application.Entries.Create.CreateEntryRequest;

namespace CashFlow.Api.Controllers;

/// <summary>
/// Disponibiliza as operações relacionadas aos lançamentos financeiros.
/// </summary>
[ApiController]
[Route("api/entries")]
public sealed class EntriesController : ControllerBase
{
    private readonly CreateEntryUseCase _createEntryUseCase;

    public EntriesController(CreateEntryUseCase createEntryUseCase)
    {
        _createEntryUseCase = createEntryUseCase;
    }

    [HttpPost]
    [ProducesResponseType<CreateEntryResult>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateEntryResult>> CreateAsync(ApiRequest request, CancellationToken cancellationToken)
    {
        // Converte o contrato HTTP para o contrato utilizado pela aplicação.
        var applicationRequest = new ApplicationRequest(
            request.Type,
            request.Amount,
            request.Description,
            request.OccurredAt);

        var result = await _createEntryUseCase.ExecuteAsync(applicationRequest, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }
}