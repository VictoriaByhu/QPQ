using Serilog.Context;

namespace Catalog.Api.Middleware;

/// <summary>
/// Додає до кожного запису логу UserId (якщо відомий) і RequestPath.
/// CorrelationId додає спільний CorrelationIdMiddleware з Platform.ServiceDefaults.
/// Секрети (токени, паролі) не логуються.
/// </summary>
public sealed class RequestContextMiddleware(RequestDelegate next)
{
    private const string UserHeader = "X-User-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User.FindFirst("sub")?.Value
                     ?? context.Request.Headers[UserHeader].FirstOrDefault();

        using (LogContext.PushProperty("UserId", userId ?? "anonymous"))
        using (LogContext.PushProperty("RequestPath", context.Request.Path.Value))
        {
            await next(context);
        }
    }
}
