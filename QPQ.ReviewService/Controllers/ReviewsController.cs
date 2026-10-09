using Microsoft.AspNetCore.Mvc;
using QPQ.ReviewService.Models;
using QPQ.ReviewService.Services;

namespace QPQ.ReviewService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly ReviewDbService _dbService;

        public ReviewsController(ReviewDbService dbService)
        {
            _dbService = dbService;
        }

        [HttpGet]
        public async Task<List<Review>> Get() =>
            await _dbService.GetReviewsAsync();

        [HttpPost]
        public async Task<IActionResult> Post(Review newReview)
        {
            // Валідація згідно з вимогами лаби
            if (newReview.Rating < 1 || newReview.Rating > 5)
                return BadRequest("Рейтинг має бути від 1 до 5.");

            newReview.CreatedAt = DateTime.UtcNow;
            await _dbService.CreateReviewAsync(newReview);

            return CreatedAtAction(nameof(Get), new { id = newReview.Id }, newReview);
        }
    }
}