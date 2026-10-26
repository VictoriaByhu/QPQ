using Serilog.Context;

namespace Catalog.Api.Middleware;

/// <summary>
/// Додає до кожного запису логу CorrelationId, UserId (якщо відомий) і RequestPath.
/// CorrelationId береться із заголовка X-Correlation-Id або дорівнює TraceIdentifier.
/// Секрети (токени, паролі) не логуються.
/// </summary>
public sealed class RequestContextMiddleware(RequestDelegate next)
{
    public const string CorrelationHeader = "X-Correlation-Id";
    private const string UserHeader = "X-User-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 64)
            correlationId = context.TraceIdentifier;
        context.Response.Headers[CorrelationHeader] = correlationId;

        var userId = context.User.FindFirst("sub")?.Value
                     ?? context.Request.Headers[UserHeader].FirstOrDefault();

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("UserId", userId ?? "anonymous"))
        using (LogContext.PushProperty("RequestPath", context.Request.Path.Value))
        {
            await next(context);
        }
    }
}
