using System.ComponentModel.DataAnnotations;
using MetaExchange.Core.Models;
using MetaExchange.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace MetaExchange.Api.Controllers;

[ApiController]
[Route("api/v1/meta-exchange")]
[Produces("application/json")]
public class MetaExchangeController(
    IMetaExchangeService metaExchangeService,
    ILogger<MetaExchangeController> logger)
    : ControllerBase
{
    private readonly IMetaExchangeService _metaExchangeService =
        metaExchangeService ?? throw new ArgumentNullException(nameof(metaExchangeService));

    private readonly ILogger<MetaExchangeController>
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));


    /// <summary>
    /// Calculates the optimal execution plan for the specified trading operation.
    /// </summary>
    /// <param name="type">The type of operation (buy/sell).</param>
    /// <param name="amount">The amount of BTC to calculate the plan for.</param>
    /// <returns>A list of planned orders that would execute the trade.</returns>
    /// <response code="200">Returns the calculated execution plan.</response>
    /// <response code="400">If the amount is invalid or operation type is invalid.</response>
    /// <response code="422">If there is insufficient liquidity.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpGet]
    [Route("plan")]
    [ProducesResponseType(typeof(IReadOnlyList<ExecutedOrder>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public ActionResult<IReadOnlyList<ExecutedOrder>> GetTradePlan(
        [FromQuery] [Required] string type,
        [FromQuery] [Required] decimal amount)
    {
        if (!new[] { "buy", "sell" }.Contains(type.ToLowerInvariant()))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Operation Type",
                Detail = "Operation type must be either 'buy' or 'sell'",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            _logger.LogInformation("Calculating {Type} execution plan for {Amount} BTC", type, amount);

            var executionPlan =
                type.Equals("buy", StringComparison.InvariantCultureIgnoreCase)
                    ? _metaExchangeService.GetBestBuyExecutionPlan(amount)
                    : _metaExchangeService.GetBestSellExecutionPlan(amount);

            return Ok(executionPlan);
        }
        catch (ArgumentException ex) when (IsTradeAmountError(ex))
        {
            _logger.LogWarning(ex, "Invalid trade amount for {Type}: {Amount} BTC", type, amount);
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Trade Amount",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Insufficient liquidity"))
        {
            _logger.LogWarning(ex, "Insufficient liquidity for {Type} amount: {Amount} BTC", type, amount);
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Insufficient Liquidity",
                Detail = ex.Message,
                Status = StatusCodes.Status422UnprocessableEntity
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating {Type} plan for amount: {Amount} BTC", type, amount);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while calculating the execution plan",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    private static bool IsTradeAmountError(ArgumentException ex) =>
        ex.Message.Contains("Amount must be greater than 0") ||
        ex.Message.Contains("below minimum trade size") ||
        ex.Message.Contains("exceeds maximum trade size");
}