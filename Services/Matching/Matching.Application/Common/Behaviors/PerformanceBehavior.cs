using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matching.Application.Common.Behaviors;

/// <summary>Вимірює час виконання обробника (разом із запитами до MongoDB) і попереджає про повільні запити.</summary>
public sealed class PerformanceBehavior<TRequest, TResponse>(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public const int SlowRequestMilliseconds = 500;

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var response = await next(ct);
        sw.Stop();

        if (sw.ElapsedMilliseconds > SlowRequestMilliseconds)
            logger.LogWarning("Slow request {RequestName}: {Elapsed} ms", typeof(TRequest).Name, sw.ElapsedMilliseconds);
        return response;
    }
}
