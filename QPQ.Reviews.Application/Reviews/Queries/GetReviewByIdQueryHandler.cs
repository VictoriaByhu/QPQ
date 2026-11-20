using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using QPQ.Reviews.Application.Interfaces;
using QPQ.Reviews.Application.Caching;
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
    private readonly CacheMetrics _metrics;

    public GetReviewByIdQueryHandler(
        IReviewQueries queries,
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<GetReviewByIdQueryHandler> logger,
        CacheMetrics metrics)
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

        var memoryCacheOptions = new MemoryCacheEntryOptions
        {
            Size = 1,
            SlidingExpiration = TimeSpan.FromMinutes(5),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };

        // ÊÐÎÊ 1. Ïåðåâ³ðÿºìî L1 (IMemoryCache)
        if (_memoryCache.TryGetValue(cacheKey, out ReviewDto? cachedReview))
        {
            _logger.LogInformation("L1 Cache hit for {CacheKey}", cacheKey);
            _metrics.Hit("L1");
            return cachedReview;
        }
        _metrics.Miss("L1");

        // ÊÐÎÊ 2. Ïåðåâ³ðÿºìî L2 (Redis) ³ç graceful degradation
        string? redisData = null;
        try
        {
            redisData = await _distributedCache.GetStringAsync(cacheKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable on GET, falling back to MongoDB for {CacheKey}", cacheKey);
        }

        if (!string.IsNullOrEmpty(redisData))
        {
            _logger.LogInformation("L2 Cache hit for {CacheKey}", cacheKey);
            _metrics.Hit("L2");

            cachedReview = JsonSerializer.Deserialize<ReviewDto>(redisData);
            _memoryCache.Set(cacheKey, cachedReview, memoryCacheOptions);

            return cachedReview;
        }
        _metrics.Miss("L2");

        _logger.LogInformation("Cache miss for {CacheKey}. Fetching from DB...", cacheKey);

        // ÊÐÎÊ 3. Éäåìî â ÁÄ
        var review = await _queries.GetByIdAsync(request.Id);

        if (review != null)
        {
            // ÊÐÎÊ 4. Çáåð³ãàºìî ó L2 (Redis) ³ç graceful degradation
            try
            {
                var serializedReview = JsonSerializer.Serialize(review);
                var distributedCacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                };
                await _distributedCache.SetStringAsync(cacheKey, serializedReview, distributedCacheOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis unavailable on SET, value for {CacheKey} not cached", cacheKey);
            }

            // ÊÐÎÊ 5. Çáåð³ãàºìî â L1 (Ïàì'ÿòü)
            _memoryCache.Set(cacheKey, review, memoryCacheOptions);
        }

        return review;
    }
}