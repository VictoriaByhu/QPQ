using Microsoft.AspNetCore.Mvc;
using UserSkills.Bll.Dtos;
using UserSkills.Bll.Services;

namespace UserSkills.Api.Controllers;

/// <summary>Довідники лише для читання: навички (копія з Catalog) і мови.</summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
public class ReferenceController(IReferenceDataService reference) : ControllerBase
{
    /// <summary>Локальна копія довідника навичок.</summary>
    [HttpGet("skills")]
    [ProducesResponseType<IReadOnlyList<SkillDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SkillDto>>> GetSkills(CancellationToken ct)
        => Ok(await reference.GetSkillsAsync(ct));

    [HttpGet("skills/{id:int}")]
    [ProducesResponseType<SkillDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SkillDto>> GetSkill(int id, CancellationToken ct)
        => Ok(await reference.GetSkillAsync(id, ct));

    [HttpGet("languages")]
    [ProducesResponseType<IReadOnlyList<LanguageRefDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LanguageRefDto>>> GetLanguages(CancellationToken ct)
        => Ok(await reference.GetLanguagesAsync(ct));

    [HttpGet("languages/{code}")]
    [ProducesResponseType<LanguageRefDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LanguageRefDto>> GetLanguage(string code, CancellationToken ct)
        => Ok(await reference.GetLanguageAsync(code, ct));
}
