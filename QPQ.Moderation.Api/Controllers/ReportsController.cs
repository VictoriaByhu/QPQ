using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using QPQ.Moderation.Bll.DTOs;
using QPQ.Moderation.Bll.Interfaces;

namespace QPQ.Moderation.Api.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ReportDto>>> GetPaged(
    [FromQuery] string? status,
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10,
    CancellationToken ct = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 10;

        var pagedResult = await _reportService.GetPagedAsync(status, pageNumber, pageSize, ct);

        return Ok(pagedResult);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReportDto>> GetById(int id, CancellationToken ct)
    {
        var report = await _reportService.GetByIdAsync(id, ct);

        if (report == null)
            return NotFound(); // Повертаємо 404, якщо скаргу не знайдено

        return Ok(report); // Повертаємо 200 та саму скаргу у вигляді DTO
    }

    [HttpPost]
    public async Task<ActionResult<ReportDto>> Create(
        [FromBody] CreateReportDto dto,
        [FromServices] IValidator<CreateReportDto> validator, // 1. Інжектимо валідатор напряму в метод
        CancellationToken ct)
    {
        // 2. Викликаємо перевірку даних явно
        var validationResult = await validator.ValidateAsync(dto, ct);

        // 3. Якщо є помилки, повертаємо стандартизований ValidationProblemDetails (400)
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        // 4. Якщо дані правильні, створюємо скаргу
        var createdReport = await _reportService.CreateAsync(dto, ct);

        // Повертаємо статус 201 Created та посилання на щойно створений ресурс
        return CreatedAtAction(nameof(GetById), new { id = createdReport.Id }, createdReport);
    }

    [HttpGet("{id:long}/explicit")]
    public async Task<ActionResult<ReportDto>> GetExplicit(long id, CancellationToken ct)
    {
        var report = await _reportService.GetWithExplicitLoadingAsync(id, ct);
        return Ok(report);
    }
}