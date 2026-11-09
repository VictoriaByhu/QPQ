using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using QPQ.Reviews.Application.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace QPQ.Reviews.Application.Reviews.Commands.DeleteReview;

public class DeleteReviewCommandHandler : IRequestHandler<DeleteReviewCommand>
{
    private readonly IReviewRepository _repository;
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<DeleteReviewCommandHandler> _logger;

    public DeleteReviewCommandHandler(
        IReviewRepository repository,
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<DeleteReviewCommandHandler> logger)
    {
        _repository = repository;
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
    }

    public async Task Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        await _repository.DeleteAsync(request.Id);

        // ЛАБ 06: Інвалідація кешу синхронно з операцією видалення
        var cacheKey = $"reviews:review:{request.Id}";
        _memoryCache.Remove(cacheKey);
        await _distributedCache.RemoveAsync(cacheKey, cancellationToken);

        _logger.LogInformation("Cache invalidated {Key}, reason {Reason}", cacheKey, "ReviewDeleted");
    }
}