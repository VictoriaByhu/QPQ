using Microsoft.AspNetCore.Mvc;
using Sessions.Api.Services;
using Sessions.Bll.Dtos;
using Sessions.Bll.Services;

namespace Sessions.Api.Controllers;

[ApiController]
[Route("api/accepted-swaps")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public class AcceptedSwapsController(IAcceptedSwapService swaps, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Lists accepted swaps of the current user.")]
    [ProducesResponseType<IReadOnlyList<AcceptedSwapSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<AcceptedSwapSummaryDto>>> GetMine(
        [FromQuery] string? status, CancellationToken ct)
        => Ok(await swaps.GetMineAsync(currentUser.UserId, status, ct));

    [HttpGet("{swapId:int}/sessions")]
    [EndpointSummary("Returns an accepted swap with its sessions.")]
    [ProducesResponseType<AcceptedSwapDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AcceptedSwapDto>> GetWithSessions(int swapId, CancellationToken ct)
        => Ok(await swaps.GetWithSessionsAsync(currentUser.UserId, swapId, ct));
}
