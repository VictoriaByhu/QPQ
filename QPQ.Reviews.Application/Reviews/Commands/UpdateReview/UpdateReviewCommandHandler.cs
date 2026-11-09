using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using QPQ.Reviews.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QPQ.Reviews.Application.Reviews.Commands.UpdateReview;

public class UpdateReviewCommandHandler : IRequestHandler<UpdateReviewCommand>
{
    private readonly IReviewRepository _repository;
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<UpdateReviewCommandHandler> _logger;

    public UpdateReviewCommandHandler(
        IReviewRepository repository,
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<UpdateReviewCommandHandler> logger)
    {
        _repository = repository;
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
    }

    public async Task Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await _repository.GetByIdAsync(request.Id);

        if (review != null)
        {
            review.Rating = request.Rating;
            review.Comment = request.Comment;

            await _repository.UpdateAsync(review);

            // ЛАБ 06: Інвалідація кешу синхронно з операцією запису
            var cacheKey = $"reviews:review:{request.Id}";
            _memoryCache.Remove(cacheKey);
            await _distributedCache.RemoveAsync(cacheKey, cancellationToken);

            _logger.LogInformation("Cache invalidated {Key}, reason {Reason}", cacheKey, "ReviewUpdated");
        }
    }
}