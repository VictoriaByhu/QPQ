using Microsoft.AspNetCore.Mvc;
using UserSkills.Api.Services;
using UserSkills.Bll.Dtos;
using UserSkills.Bll.Services;

namespace UserSkills.Api.Controllers;

/// <summary>Навички поточного користувача: що пропоную навчати (Offer) і що хочу вивчити (Want).</summary>
[ApiController]
[Route("api/user-skills")]
[Produces("application/json")]
public class UserSkillsController(IUserSkillService userSkills, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Список навичок поточного користувача з фільтрами та пагінацією.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserSkillSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<UserSkillSummaryDto>>> List(
        [FromQuery] string? type, [FromQuery] string? status,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await userSkills.ListAsync(currentUser.UserId, type, status, pageNumber, pageSize, ct));

    /// <summary>Запис із деталями, мовами та доступністю.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<UserSkillDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserSkillDto>> GetById(int id, CancellationToken ct)
        => Ok(await userSkills.GetByIdAsync(id, ct));

    /// <summary>Додати навичку (запис, деталі, мови, доступність і журнал — в одній транзакції).</summary>
    [HttpPost]
    [ProducesResponseType<UserSkillDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserSkillDto>> Create(CreateUserSkillDto input, CancellationToken ct)
    {
        var created = await userSkills.CreateAsync(currentUser.UserId, input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Оновити рівень, деталі, мови й доступність (потрібна актуальна RowVersion).</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<UserSkillDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserSkillDto>> Update(
        int id, UpdateUserSkillDto input, CancellationToken ct)
        => Ok(await userSkills.UpdateAsync(currentUser.UserId, id, input, ct));

    /// <summary>Змінити статус: Active/Paused -> Archived, Active &lt;-&gt; Paused. Archived — кінцевий.</summary>
    [HttpPost("{id:int}/status")]
    [ProducesResponseType<RowVersionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RowVersionDto>> ChangeStatus(
        int id, ChangeStatusDto input, CancellationToken ct)
        => Ok(await userSkills.ChangeStatusAsync(currentUser.UserId, id, input, ct));

    /// <summary>
    /// М'яке видалення. RowVersion — base64 з query-рядка (символ + потрібно передавати як %2B).
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, [FromQuery] string rowVersion, CancellationToken ct)
    {
        await userSkills.DeleteAsync(currentUser.UserId, id, DecodeRowVersion(rowVersion), ct);
        return NoContent();
    }

    /// <summary>Журнал змін статусу (лише власник).</summary>
    [HttpGet("{id:int}/history")]
    [ProducesResponseType<IReadOnlyList<StatusHistoryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<StatusHistoryDto>>> GetHistory(
        int id, CancellationToken ct)
        => Ok(await userSkills.GetHistoryAsync(currentUser.UserId, id, ct));

    private static byte[] DecodeRowVersion(string? value)
    {
        // У query-рядку '+' може прийти як пробіл.
        var normalized = (value ?? string.Empty).Replace(' ', '+');
        try
        {
            return Convert.FromBase64String(normalized);
        }
        catch (FormatException)
        {
            throw new Domain.Exceptions.ValidationException("rowVersion must be a base64 string.");
        }
    }
}
