using MediatR;
using Microsoft.AspNetCore.Mvc;
using Matching.Application.Common.Models;
using Matching.Application.SkillStats.Commands.RebuildSkillStats;
using Matching.Application.SkillStats.Queries.GetSkillStats;
using Matching.Application.SkillStats.Queries.GetSkillStatsList;

namespace Matching.WebApi.Controllers;

/// <summary>Похідна колекція skillStats: попит і пропозиція за навичками.</summary>
[Route("api/skill-stats")]
[Produces("application/json")]
public sealed class SkillStatsController(ISender mediator) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SkillStatsDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SkillStatsDto>>> GetAll(CancellationToken ct)
        => Ok(await mediator.Send(new GetSkillStatsListQuery(), ct));

    [HttpGet("{skillId:int}")]
    [ProducesResponseType<SkillStatsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SkillStatsDto>> GetBySkill(int skillId, CancellationToken ct)
        => Ok(await mediator.Send(new GetSkillStatsQuery(skillId), ct));

    /// <summary>Перераховує лічильники агрегацією ($group + $merge). Повертає кількість документів.</summary>
    [HttpPost("rebuild")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> Rebuild(CancellationToken ct)
        => Ok(await mediator.Send(new RebuildSkillStatsCommand(), ct));
}
