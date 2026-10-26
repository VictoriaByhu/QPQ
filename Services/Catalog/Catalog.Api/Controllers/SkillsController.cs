using Catalog.Bll.Dtos;
using Catalog.Bll.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

/// <summary>Довідник навичок.</summary>
[Route("api/skills")]
public class SkillsController(ISkillService service) : ApiControllerBase
{
    /// <summary>Список навичок: пагінація, пошук, фільтр за категорією й тегом, сортування (виконуються в SQL).</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<SkillListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SkillListItemDto>>> GetList(
        [FromQuery] SkillQuery query, CancellationToken ct)
    {
        var result = await service.GetListAsync(query, ct);
        AddPagingHeaders(result.TotalCount, result.Page, result.PageSize, result.TotalPages);
        return Ok(result);
    }

    /// <summary>Навичка з категорією, описом, рівнями й тегами (Eager Loading).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<SkillDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SkillDto>> GetById(int id, CancellationToken ct)
        => Ok(await service.GetByIdAsync(id, ct));

    /// <summary>Навички, яким тег призначили не раніше вказаної дати (M:N через проміжну сутність).</summary>
    [HttpGet("by-tag")]
    [ProducesResponseType<IReadOnlyList<SkillBriefDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SkillBriefDto>>> GetByTag(
        [FromQuery] string tagName, [FromQuery] DateTime? since, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tagName))
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["tagName"] = ["tagName is required."] }));

        return Ok(await service.GetRecentlyTaggedAsync(tagName, since ?? DateTime.MinValue, ct));
    }

    [HttpPost]
    [ProducesResponseType<SkillDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SkillDto>> Create(
        CreateSkillDto input, [FromServices] IValidator<CreateSkillDto> validator, CancellationToken ct)
    {
        if (await ValidateAsync(validator, input, ct) is { } invalid) return invalid;

        var created = await service.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<SkillDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SkillDto>> Update(
        int id, UpdateSkillDto input, [FromServices] IValidator<UpdateSkillDto> validator, CancellationToken ct)
    {
        if (await ValidateAsync(validator, input, ct) is { } invalid) return invalid;
        return Ok(await service.UpdateAsync(id, input, ct));
    }

    /// <summary>М'яке видалення навички.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/tags/{tagId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddTag(int id, int tagId, CancellationToken ct)
    {
        await service.AddTagAsync(id, tagId, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}/tags/{tagId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveTag(int id, int tagId, CancellationToken ct)
    {
        await service.RemoveTagAsync(id, tagId, ct);
        return NoContent();
    }

    /// <summary>Змінити назву й опис рівня володіння (1..5) для навички.</summary>
    [HttpPut("{id:int}/levels/{level:int}")]
    [ProducesResponseType<SkillLevelDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SkillLevelDto>> UpdateLevel(
        int id, int level, UpdateSkillLevelDto input,
        [FromServices] IValidator<UpdateSkillLevelDto> validator, CancellationToken ct)
    {
        if (level is < 1 or > 5)
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["level"] = ["Level must be between 1 and 5."] }));
        if (await ValidateAsync(validator, input, ct) is { } invalid) return invalid;

        return Ok(await service.UpdateLevelAsync(id, (byte)level, input, ct));
    }
}
