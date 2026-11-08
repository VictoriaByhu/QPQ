using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Platform.Aggregator.Dtos;
using Platform.Aggregator.Services;
using Polly.CircuitBreaker;
using Polly.Timeout;
using GrpcStatusCode = Grpc.Core.StatusCode;

namespace Platform.Aggregator.Controllers;

[ApiController]
[Route("api/aggregator")]
[Produces("application/json")]
public sealed class AggregatorController(FullSwapService fullSwaps) : ControllerBase
{
    [HttpGet("swaps/{id:int}/full")]
    [EndpointSummary("Full swap card: the swap with its sessions and conversation messages.")]
    [ProducesResponseType<FullSwapDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
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
        catch (RpcException ex) when (ex.StatusCode == GrpcStatusCode.Unauthenticated)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "swaps-api rejected the request",
                detail: "Provide the X-User-Id header of a swap participant.");
        }
        catch (RpcException ex) when (ex.StatusCode == GrpcStatusCode.InvalidArgument)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "swaps-api rejected the request",
                detail: ex.Status.Detail);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested && ex is RpcException
                                       or HttpRequestException
                                       or TaskCanceledException
                                       or TimeoutRejectedException
                                       or BrokenCircuitException)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "swaps-api is unavailable");
        }
    }
}
