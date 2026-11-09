using Microsoft.AspNetCore.Mvc;
using Platform.Aggregator.Models;
using Platform.Aggregator.Services;

namespace Platform.Aggregator.Controllers;

[ApiController]
[Route("api/aggregator")]
public sealed class AggregatorController(ISkillAggregationService aggregation) : ControllerBase
{
    /// <summary>Повна картка навички: довідник + записи користувачів + статистика пошуку пар.</summary>
    [HttpGet("skills/{id:int}/full")]
    [ProducesResponseType<FullSkillDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<FullSkillDto>> GetFullSkill(int id, CancellationToken ct)
    {
        try
        {
            var result = await aggregation.GetFullSkillAsync(id, ct);
            return result is null
                ? NotFound(new ProblemDetails { Status = 404, Title = "Skill not found", Detail = $"Skill {id} does not exist in catalog-api." })
                : Ok(result);
        }
        catch (UpstreamUnavailableException ex)
        {
            return Problem(statusCode: 503, title: $"{ex.Service} is unavailable",
                detail: ex.Message);
        }
    }
}
