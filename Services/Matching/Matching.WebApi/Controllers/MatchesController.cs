using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Matching.Application.Common.Models;
using Matching.Application.Matches.Commands.GenerateMatches;
using Matching.Application.Matches.Commands.UpdateMatchStatus;
using Matching.Application.Matches.Queries.GetMatchById;
using Matching.Application.Matches.Queries.GetMatchesList;
using Matching.WebApi.Contracts;

namespace Matching.WebApi.Controllers;

/// <summary>Знайдені пари. Користувач бачить лише пари, у яких він учасник (userIds).</summary>
[Route("api/matches")]
[Produces("application/json")]
public sealed class MatchesController(ISender mediator, IMapper mapper) : ApiControllerBase
{
    /// <summary>Пари користувача за спаданням оцінки.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<MatchDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<MatchDto>>> GetForUser(
        [FromQuery] string userId, [FromQuery] bool includeDismissed = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetMatchesListQuery(userId, includeDismissed, page, pageSize), ct));

    [HttpGet("{id}")]
    [ProducesResponseType<MatchDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MatchDto>> GetById(string id, CancellationToken ct)
    {
        var match = await mediator.Send(new GetMatchByIdQuery(id), ct);
        SetETag(match.Version);
        return Ok(match);
    }

    /// <summary>Підбирає нові пари для користувача (ідемпотентно: наявні пари пропускаються).</summary>
    [HttpPost("generate")]
    [ProducesResponseType<GenerateMatchesResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GenerateMatchesResult>> Generate(GenerateMatchesRequest request, CancellationToken ct)
        => Ok(await mediator.Send(mapper.Map<GenerateMatchesCommand>(request), ct));

    /// <summary>Позначає пару переглянутою або відхиленою (If-Match обов'язковий).</summary>
    [HttpPut("{id}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status428PreconditionRequired)]
    public async Task<IActionResult> UpdateStatus(
        string id, UpdateMatchStatusRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken ct)
    {
        if (!TryReadIfMatch(ifMatch, out var expectedVersion, out var error)) return error!;

        var command = mapper.Map<UpdateMatchStatusCommand>(request) with { Id = id, ExpectedVersion = expectedVersion };
        SetETag(await mediator.Send(command, ct));
        return NoContent();
    }
}
