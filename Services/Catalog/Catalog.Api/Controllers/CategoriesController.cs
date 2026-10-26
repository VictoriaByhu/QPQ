using Catalog.Bll.Dtos;
using Catalog.Bll.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

/// <summary>Категорії навичок.</summary>
[Route("api/categories")]
public class CategoriesController(ICategoryService service) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<CategoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CategoryDto>>> GetList(
        [FromQuery] CategoryQuery query, CancellationToken ct)
    {
        var result = await service.GetListAsync(query, ct);
        AddPagingHeaders(result.TotalCount, result.Page, result.PageSize, result.TotalPages);
        return Ok(result);
    }

    /// <summary>Категорія з її навичками (Explicit Loading: два окремі SQL-запити).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<CategoryWithSkillsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryWithSkillsDto>> GetById(int id, CancellationToken ct)
        => Ok(await service.GetByIdAsync(id, ct));

    [HttpPost]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Create(
        CreateCategoryDto input, [FromServices] IValidator<CreateCategoryDto> validator, CancellationToken ct)
    {
        if (await ValidateAsync(validator, input, ct) is { } invalid) return invalid;

        var created = await service.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Update(
        int id, UpdateCategoryDto input, [FromServices] IValidator<UpdateCategoryDto> validator, CancellationToken ct)
    {
        if (await ValidateAsync(validator, input, ct) is { } invalid) return invalid;
        return Ok(await service.UpdateAsync(id, input, ct));
    }

    /// <summary>М'яке видалення; категорія з активними навичками не видаляється (409).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
