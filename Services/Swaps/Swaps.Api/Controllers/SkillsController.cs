using Microsoft.AspNetCore.Mvc;
using Swaps.Bll.Dtos;
using Swaps.Bll.Services;

namespace Swaps.Api.Controllers;

[ApiController]
[Route("api/skills")]
[Produces("application/json")]
public class SkillsController(ISkillService skills) : ControllerBase
{
    /// <summary>Lists skills (local copy of the Catalog service data).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SkillDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SkillDto>>> GetAll(CancellationToken ct)
        => Ok(await skills.GetAllAsync(ct));

    /// <summary>Returns a skill by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<SkillDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SkillDto>> GetById(int id, CancellationToken ct)
        => Ok(await skills.GetByIdAsync(id, ct));
}
