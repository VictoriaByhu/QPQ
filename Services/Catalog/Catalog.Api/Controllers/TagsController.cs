using Catalog.Bll.Dtos;
using Catalog.Bll.Services;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

/// <summary>Теги навичок.</summary>
[Route("api/tags")]
public class TagsController(ITagService service) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TagDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TagDto>>> GetAll(CancellationToken ct)
        => Ok(await service.GetAllAsync(ct));

    [HttpPost]
    [ProducesResponseType<TagDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TagDto>> Create(CreateTagDto input, CancellationToken ct)
    {
        var created = await service.CreateAsync(input, ct);
        return Created($"/api/tags", created);
    }
}
