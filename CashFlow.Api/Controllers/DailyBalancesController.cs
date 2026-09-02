using CashFlow.Application.DailyBalances.Get;
using CashFlow.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Api.Controllers;

/// <summary>
/// Disponibiliza as operações relacionadas ao consolidado diário.
/// </summary>
[ApiController]
[Route("api/daily-balances")]
public sealed class DailyBalancesController : ControllerBase
{
    private readonly GetDailyBalanceUseCase _getDailyBalanceUseCase;

    public DailyBalancesController(GetDailyBalanceUseCase getDailyBalanceUseCase)
    {
        _getDailyBalanceUseCase = getDailyBalanceUseCase;
    }

    [HttpGet("{date}")]
    [ProducesResponseType<DailyBalance>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DailyBalance>> GetAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var result = await _getDailyBalanceUseCase.ExecuteAsync(date, cancellationToken);
        return Ok(result);
    }
}