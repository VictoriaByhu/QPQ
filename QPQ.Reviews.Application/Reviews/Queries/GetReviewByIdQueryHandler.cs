using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using QPQ.Reviews.Application.Interfaces;
using QPQ.Reviews.Application.Caching; // Підключення наших метрик
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace QPQ.Reviews.Application.Reviews.Queries.GetReviewById;

public class GetReviewByIdQueryHandler : IRequestHandler<GetReviewByIdQuery, ReviewDto?>
{
    private readonly IReviewQueries _queries;
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<GetReviewByIdQueryHandler> _logger;
    private readonly CacheMetrics _metrics; // Додано метрики

    public GetReviewByIdQueryHandler(
        IReviewQueries queries,
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<GetReviewByIdQueryHandler> logger,
        CacheMetrics metrics) // Ін'єкція метрик
    {
        _queries = queries;
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<ReviewDto?> Handle(GetReviewByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"reviews:review:{request.Id}";

        // Опції для IMemoryCache (Виправлення помилки SizeLimit)
        var memoryCacheOptions = new MemoryCacheEntryOptions
        {
            Size = 1, // Обов'язково вказуємо розмір
            SlidingExpiration = TimeSpan.FromMinutes(5),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };

        // КРОК 1. Перевіряємо L1 (IMemoryCache)
        if (_memoryCache.TryGetValue(cacheKey, out ReviewDto? cachedReview))
        {
            _logger.LogInformation("L1 Cache hit for {CacheKey}", cacheKey);
            _metrics.Hit("L1"); // Реєструємо попадання L1
            return cachedReview;
        }
        _metrics.Miss("L1"); // Реєструємо промах L1

        // КРОК 2. Перевіряємо L2 (Redis)
        var redisData = await _distributedCache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(redisData))
        {
            _logger.LogInformation("L2 Cache hit for {CacheKey}", cacheKey);
            _metrics.Hit("L2"); // Реєструємо попадання L2

            cachedReview = JsonSerializer.Deserialize<ReviewDto>(redisData);
            _memoryCache.Set(cacheKey, cachedReview, memoryCacheOptions);

            return cachedReview;
        }
        _metrics.Miss("L2"); // Реєструємо промах L2

        _logger.LogInformation("Cache miss for {CacheKey}. Fetching from DB...", cacheKey);

        // КРОК 3. Йдемо в БД
        var review = await _queries.GetByIdAsync(request.Id);

        if (review != null)
        {
            // КРОК 4. Зберігаємо у L2 (Redis)
            var serializedReview = JsonSerializer.Serialize(review);
            var distributedCacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };
            await _distributedCache.SetStringAsync(cacheKey, serializedReview, distributedCacheOptions, cancellationToken);

            // КРОК 5. Зберігаємо в L1 (Пам'ять) з правильними опціями
            _memoryCache.Set(cacheKey, review, memoryCacheOptions);
        }

        return review;
    }
}