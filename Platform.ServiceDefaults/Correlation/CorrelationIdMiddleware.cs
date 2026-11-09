using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Platform.ServiceDefaults.Correlation;

/// <summary>
/// Наскрізний ідентифікатор запиту (бізнес-кореляція; не плутати з TraceId від OpenTelemetry).
/// Якщо заголовка X-Correlation-Id немає — генерується новий GUID (у Gateway це відбувається першим).
/// Значення потрапляє в LogContext (усі логи запиту), у заголовок відповіді та в тег поточного span.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var generated = false;
        if (!context.Request.Headers.TryGetValue(HeaderName, out var value) || string.IsNullOrWhiteSpace(value))
        {
            value = Guid.NewGuid().ToString();
            context.Request.Headers[HeaderName] = value;          // далі його побачать YARP і HttpClient
            generated = true;
        }

        var correlationId = value.ToString();
        context.Response.Headers[HeaderName] = correlationId;
        Activity.Current?.SetTag("correlation.id", correlationId); // пошук trace за CorrelationId у Dashboard

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            if (generated)
                logger.LogInformation("Generated new CorrelationId {CorrelationId} for {Method} {Path}",
                    correlationId, context.Request.Method, context.Request.Path);

            logger.LogInformation("Request started {Method} {Path}", context.Request.Method, context.Request.Path);
            try
            {
                await next(context);
            }
            finally
            {
                logger.LogInformation("Request finished with {StatusCode}", context.Response.StatusCode);
            }
        }
    }
}
