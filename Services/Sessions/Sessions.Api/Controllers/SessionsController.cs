using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Sessions.Api.Services;
using Sessions.Api.Validation;
using Sessions.Bll.Dtos;
using Sessions.Bll.Services;

namespace Sessions.Api.Controllers;

[ApiController]
[Route("api/sessions")]
[Produces("application/json")]
[ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public class SessionsController(ISessionService sessions, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Lists sessions of the current user with filtering, sorting and paging.")]
    [ProducesResponseType<PagedResult<SessionListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SessionListItemDto>>> GetList(
        [FromQuery] SessionQueryDto query,
        [FromServices] IValidator<SessionQueryDto> validator,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
            return this.ToValidationProblem(validation);

        return Ok(await sessions.GetListAsync(currentUser.UserId, query, ct));
    }

    [HttpGet("upcoming")]
    [EndpointSummary("Returns the nearest planned sessions of the current user.")]
    [ProducesResponseType<IReadOnlyList<SessionListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SessionListItemDto>>> GetUpcoming(
        [FromQuery] int take = 5, CancellationToken ct = default)
        => Ok(await sessions.GetUpcomingAsync(currentUser.UserId, take, ct));

    [HttpGet("{id:int}")]
    [EndpointSummary("Returns a session with its format, skills and outcome.")]
    [ProducesResponseType<SessionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionDto>> GetById(int id, CancellationToken ct)
        => Ok(await sessions.GetByIdAsync(currentUser.UserId, id, ct));

    [HttpPost]
    [EndpointSummary("Plans a session for an accepted swap.")]
    [ProducesResponseType<SessionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SessionDto>> Create(
        CreateSessionDto input,
        [FromServices] IValidator<CreateSessionDto> validator,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(input, ct);
        if (!validation.IsValid)
            return this.ToValidationProblem(validation);

        var created = await sessions.CreateAsync(currentUser.UserId, input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [EndpointSummary("Reschedules a planned session or changes its format and place.")]
    [ProducesResponseType<SessionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SessionDto>> Update(
        int id,
        UpdateSessionDto input,
        [FromServices] IValidator<UpdateSessionDto> validator,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(input, ct);
        if (!validation.IsValid)
            return this.ToValidationProblem(validation);

        return Ok(await sessions.UpdateAsync(currentUser.UserId, id, input, ct));
    }

    [HttpPost("{id:int}/complete")]
    [EndpointSummary("Completes a planned session and records its outcome.")]
    [ProducesResponseType<SessionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SessionDto>> Complete(int id, CompleteSessionDto input, CancellationToken ct)
        => Ok(await sessions.CompleteAsync(currentUser.UserId, id, input, ct));

    [HttpPost("{id:int}/cancel")]
    [EndpointSummary("Cancels a planned session.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        int id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ChangeSessionStatusDto? input,
        CancellationToken ct)
    {
        await sessions.CancelAsync(currentUser.UserId, id, input, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [EndpointSummary("Soft-deletes a session that is not completed.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await sessions.DeleteAsync(currentUser.UserId, id, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/skills")]
    [EndpointSummary("Adds a skill to a planned session.")]
    [ProducesResponseType<SessionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SessionDto>> AddSkill(int id, AddSessionSkillDto input, CancellationToken ct)
        => Ok(await sessions.AddSkillAsync(currentUser.UserId, id, input, ct));

    [HttpDelete("{id:int}/skills/{skillId:int}")]
    [EndpointSummary("Removes a skill from a planned session.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveSkill(int id, int skillId, CancellationToken ct)
    {
        await sessions.RemoveSkillAsync(currentUser.UserId, id, skillId, ct);
        return NoContent();
    }
}
