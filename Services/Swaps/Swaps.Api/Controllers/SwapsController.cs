using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swaps.Api.Services;
using Swaps.Bll.Dtos;
using Swaps.Bll.Services;

namespace Swaps.Api.Controllers;

[ApiController]
[Route("api/swaps")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public class SwapsController(ISwapService swaps, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Lists swaps of the current user with optional filters and paging.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SwapListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SwapListItemDto>>> GetMine(
        [FromQuery] SwapQueryDto query, CancellationToken ct)
        => Ok(await swaps.GetMineAsync(currentUser.UserId, query, ct));

    /// <summary>Returns a swap with its details and skills.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<SwapDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SwapDto>> GetById(int id, CancellationToken ct)
        => Ok(await swaps.GetByIdAsync(currentUser.UserId, id, ct));

    /// <summary>Creates a swap (swap, details, skills and history in one transaction).</summary>
    [HttpPost]
    [ProducesResponseType<SwapDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SwapDto>> Create(CreateSwapDto input, CancellationToken ct)
    {
        var created = await swaps.CreateAsync(currentUser.UserId, input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Updates swap details. Allowed for the initiator while the swap is Pending.</summary>
    [HttpPut("{id:int}/details")]
    [ProducesResponseType<SwapDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SwapDto>> UpdateDetails(
        int id, UpdateSwapDetailsDto input, CancellationToken ct)
        => Ok(await swaps.UpdateDetailsAsync(currentUser.UserId, id, input, ct));

    /// <summary>Soft-deletes a swap.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await swaps.DeleteAsync(currentUser.UserId, id, ct);
        return NoContent();
    }

    /// <summary>Partner accepts a Pending swap.</summary>
    [HttpPost("{id:int}/accept")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Accept(
        int id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ChangeSwapStatusDto? input,
        CancellationToken ct)
    {
        await swaps.AcceptAsync(currentUser.UserId, id, input, ct);
        return NoContent();
    }

    /// <summary>Partner rejects a Pending swap.</summary>
    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(
        int id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ChangeSwapStatusDto? input,
        CancellationToken ct)
    {
        await swaps.RejectAsync(currentUser.UserId, id, input, ct);
        return NoContent();
    }

    /// <summary>Cancels a swap (initiator while Pending, either participant once Accepted).</summary>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        int id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ChangeSwapStatusDto? input,
        CancellationToken ct)
    {
        await swaps.CancelAsync(currentUser.UserId, id, input, ct);
        return NoContent();
    }

    /// <summary>Marks an Accepted swap as Completed.</summary>
    [HttpPost("{id:int}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Complete(
        int id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ChangeSwapStatusDto? input,
        CancellationToken ct)
    {
        await swaps.CompleteAsync(currentUser.UserId, id, input, ct);
        return NoContent();
    }

    /// <summary>Returns the status history of a swap.</summary>
    [HttpGet("{id:int}/history")]
    [ProducesResponseType<IReadOnlyList<SwapStatusHistoryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SwapStatusHistoryDto>>> GetHistory(int id, CancellationToken ct)
        => Ok(await swaps.GetHistoryAsync(currentUser.UserId, id, ct));
}
