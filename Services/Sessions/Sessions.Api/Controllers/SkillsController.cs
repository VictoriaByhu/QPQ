using Microsoft.AspNetCore.Mvc;
using Sessions.Api.Services;
using Sessions.Bll.Dtos;
using Sessions.Bll.Services;

namespace Sessions.Api.Controllers;

[ApiController]
[Route("api/skills")]
[Produces("application/json")]
public class SkillsController(ISkillService skills, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Lists skills available for sessions.")]
    [ProducesResponseType<IReadOnlyList<SkillDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SkillDto>>> GetAll(CancellationToken ct)
        => Ok(await skills.GetAllAsync(ct));

    [HttpGet("{id:int}/sessions")]
    [EndpointSummary("Lists sessions of the current user that cover the skill.")]
    [ProducesResponseType<IReadOnlyList<SkillSessionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SkillSessionDto>>> GetSessions(
        int id, [FromQuery] DateTime? from, CancellationToken ct)
        => Ok(await skills.GetSessionsAsync(currentUser.UserId, id, from, ct));
}
