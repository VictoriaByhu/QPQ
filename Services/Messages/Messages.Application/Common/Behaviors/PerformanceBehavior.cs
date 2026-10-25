using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Messages.Application.Common.Behaviors;

public sealed class PerformanceBehavior<TRequest, TResponse>(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public const int SlowRequestThresholdMs = 500;

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        var elapsed = stopwatch.ElapsedMilliseconds;
        if (elapsed > SlowRequestThresholdMs)
            logger.LogWarning("Slow request {RequestName}: {ElapsedMs} ms (threshold {ThresholdMs} ms) {@Request}",
                typeof(TRequest).Name, elapsed, SlowRequestThresholdMs, request);
        else
            logger.LogDebug("{RequestName} handled in {ElapsedMs} ms", typeof(TRequest).Name, elapsed);

        return response;
    }
}
