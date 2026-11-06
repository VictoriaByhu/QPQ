using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Platform.ServiceDefaults;

public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();
        var generated = string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 64;
        if (generated)
            correlationId = Guid.NewGuid().ToString();

        context.Request.Headers[HeaderName] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });
        Activity.Current?.SetTag("correlation.id", correlationId);

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            if (generated)
                logger.LogInformation("Generated new correlation id {CorrelationId}", correlationId);

            logger.LogInformation("Request started {Method} {Path}", context.Request.Method, context.Request.Path);
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await next(context);
            }
            finally
            {
                logger.LogInformation("Request finished {Method} {Path} with {StatusCode} in {Elapsed} ms",
                    context.Request.Method, context.Request.Path, context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
