using Microsoft.AspNetCore.Mvc;
using QPQ.Moderation.Bll.DTOs;
using QPQ.Moderation.Bll.Interfaces;

namespace QPQ.Moderation.Api.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController(IReportService reportService) : ControllerBase
{
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ReportDto>> GetById(long id, CancellationToken ct)
    {
        var report = await reportService.GetReportAsync(id, ct);
        return Ok(report); // У разі успіху повертаємо статус 200 OK
    }

    [HttpPost("{id:long}/change-status")]
    public async Task<IActionResult> ChangeStatus(long id, [FromBody] ChangeStatusDto input, CancellationToken ct)
    {
        await reportService.ChangeReportStatusAsync(id, input.NewStatus, ct);
        return NoContent(); // У разі успішного оновлення повертаємо 204 No Content
    }
}