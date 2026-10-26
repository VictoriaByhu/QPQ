using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPQ.Reviews.Application.Reviews.Commands.CreateReview;
using QPQ.Reviews.Application.Reviews.Commands.DeleteReview;
using QPQ.Reviews.Application.Reviews.Commands.UpdateReview;
using QPQ.Reviews.Application.Reviews.Queries.GetReviewById;
using QPQ.Reviews.Application.Reviews.Queries.GetReviewsList;
using System.Threading.Tasks;

namespace QPQ.Reviews.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly ISender _sender;

    public ReviewsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewCommand command)
    {
        var reviewId = await _sender.Send(command);
        return CreatedAtAction(nameof(GetReview), new { id = reviewId.ToString() }, new { Id = reviewId.ToString() });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetReview(string id)
    {
        var review = await _sender.Send(new GetReviewByIdQuery(id));

        if (review == null)
            return NotFound();

        return Ok(review);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateReview(string id, [FromBody] UpdateReviewCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID in URL and body must match.");

        await _sender.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReview(string id)
    {
        await _sender.Send(new DeleteReviewCommand(id));
        return NoContent();
    }
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _sender.Send(new GetReviewsListQuery(page, pageSize));
        return Ok(result);
    }
}