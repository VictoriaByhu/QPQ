using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Matching.Application.Common.Models;
using Matching.Application.IndexEntries.Commands.CreateIndexEntry;
using Matching.Application.IndexEntries.Commands.DeleteIndexEntry;
using Matching.Application.IndexEntries.Commands.UpdateIndexEntry;
using Matching.Application.IndexEntries.Queries.GetIndexEntriesList;
using Matching.Application.IndexEntries.Queries.GetIndexEntryById;
using Matching.WebApi.Contracts;

namespace Matching.WebApi.Controllers;

/// <summary>Записи індексу підбору (локальна копія UserSkills). Контролер лише передає команди й запити в MediatR.</summary>
[Route("api/index-entries")]
[Produces("application/json")]
public sealed class IndexEntriesController(ISender mediator, IMapper mapper) : ApiControllerBase
{
    /// <summary>Список записів з фільтрами й пагінацією (skip/limit виконується в MongoDB).</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<IndexEntryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<IndexEntryDto>>> GetList(
        [FromQuery] string? userId, [FromQuery] int? skillId, [FromQuery] string? type,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetIndexEntriesListQuery(userId, skillId, type, page, pageSize), ct));

    /// <summary>Запис за Id. Версія документа повертається в заголовку ETag.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType<IndexEntryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IndexEntryDto>> GetById(string id, CancellationToken ct)
    {
        var entry = await mediator.Send(new GetIndexEntryByIdQuery(id), ct);
        SetETag(entry.Version);
        return Ok(entry);
    }

    /// <summary>Створює запис. 201 + Location, у тілі — Id.</summary>
    [HttpPost]
    [ProducesResponseType<string>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<string>> Create(CreateIndexEntryRequest request, CancellationToken ct)
    {
        var id = await mediator.Send(mapper.Map<CreateIndexEntryCommand>(request), ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Оновлює запис. Очікувана версія — в заголовку If-Match; при розбіжності 409.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status428PreconditionRequired)]
    public async Task<IActionResult> Update(
        string id, UpdateIndexEntryRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken ct)
    {
        if (!TryReadIfMatch(ifMatch, out var expectedVersion, out var error)) return error!;

        var command = mapper.Map<UpdateIndexEntryCommand>(request) with { Id = id, ExpectedVersion = expectedVersion };
        SetETag(await mediator.Send(command, ct));
        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await mediator.Send(new DeleteIndexEntryCommand(id), ct);
        return NoContent();
    }
}
