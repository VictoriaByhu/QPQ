using System.Net;
using Microsoft.AspNetCore.Mvc;
using Platform.Aggregator.Dtos;
using Platform.Aggregator.Services;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Platform.Aggregator.Controllers;

[ApiController]
[Route("api/aggregator")]
[Produces("application/json")]
public sealed class AggregatorController(FullSwapService fullSwaps) : ControllerBase
{
    [HttpGet("swaps/{id:int}/full")]
    [EndpointSummary("Full swap card: the swap with its sessions and conversation messages.")]
    [ProducesResponseType<FullSwapDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<FullSwapDto>> GetFullSwap(int id, CancellationToken ct)
    {
        try
        {
            var result = await fullSwaps.GetFullSwapAsync(id, ct);
            return result is null
                ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Swap not found", detail: $"Swap {id} not found.")
                : Ok(result);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest)
        {
            return Problem(statusCode: (int)ex.StatusCode, title: "swaps-api rejected the request",
                detail: "Provide the X-User-Id header of a swap participant.");
        }
        catch (Exception ex) when (!ct.IsCancellationRequested && ex is HttpRequestException
                                       or TaskCanceledException
                                       or TimeoutRejectedException
                                       or BrokenCircuitException)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "swaps-api is unavailable");
        }
    }
}
