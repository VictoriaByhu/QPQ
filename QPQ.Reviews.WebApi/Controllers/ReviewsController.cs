using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPQ.Reviews.Application.Reviews.Commands.CreateReview;
using QPQ.Reviews.Application.Reviews.Commands.DeleteReview;
using QPQ.Reviews.Application.Reviews.Commands.UpdateReview;
using QPQ.Reviews.Application.Reviews.Queries.GetReviewById;
using QPQ.Reviews.Application.Reviews.Queries.GetReviewsList;
using System.Diagnostics; // Обов'язково для ActivitySource
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // Обов'язково для ILogger

namespace QPQ.Reviews.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<ReviewsController> _logger;

    // 1. Створюємо джерело спанів (назва має збігатися з тим, що ми додали в ServiceDefaults)
    private static readonly ActivitySource ActivitySource = new("BusinessActivitySource");

    public ReviewsController(ISender sender, ILogger<ReviewsController> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewCommand command)
    {
        // 2. Відкриваємо власний бізнес-спан для OpenTelemetry
        using var activity = ActivitySource.StartActivity("CreateReview");

        var reviewId = await _sender.Send(command);

        // 3. Додаємо тег у спан (буде видно в Aspire Dashboard)
        activity?.SetTag("review.id", reviewId.ToString());

        // 4. Структуроване логування бізнес-події (зберігається як окрема властивість {ReviewId})
        _logger.LogInformation("Review {ReviewId} created successfully", reviewId);

        return CreatedAtAction(nameof(GetReview), new { id = reviewId.ToString() }, new { Id = reviewId.ToString() });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetReview(string id)
    {
        using var activity = ActivitySource.StartActivity("GetReviewById");
        activity?.SetTag("review.id", id);

        _logger.LogInformation("Fetching review {ReviewId}", id);

        var review = await _sender.Send(new GetReviewByIdQuery(id));

        if (review == null)
        {
            _logger.LogWarning("Review {ReviewId} not found", id);
            return NotFound();
        }

        return Ok(review);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateReview(string id, [FromBody] UpdateReviewCommand command)
    {
        using var activity = ActivitySource.StartActivity("UpdateReview");
        activity?.SetTag("review.id", id);

        if (id != command.Id)
        {
            _logger.LogWarning("Update failed: ID mismatch. URL {UrlId} vs Body {BodyId}", id, command.Id);
            return BadRequest("ID in URL and body must match.");
        }

        await _sender.Send(command);
        _logger.LogInformation("Review {ReviewId} updated successfully", id);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReview(string id)
    {
        using var activity = ActivitySource.StartActivity("DeleteReview");
        activity?.SetTag("review.id", id);

        await _sender.Send(new DeleteReviewCommand(id));
        _logger.LogInformation("Review {ReviewId} deleted successfully", id);

        return NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        using var activity = ActivitySource.StartActivity("GetReviewsList");
        activity?.SetTag("pagination.page", page);
        activity?.SetTag("pagination.pageSize", pageSize);

        _logger.LogInformation("Fetching reviews list - Page {Page}, Size {PageSize}", page, pageSize);

        var result = await _sender.Send(new GetReviewsListQuery(page, pageSize));
        return Ok(result);
    }
}