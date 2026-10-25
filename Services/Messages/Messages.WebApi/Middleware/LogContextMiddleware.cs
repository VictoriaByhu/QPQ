using Serilog.Context;
using Messages.WebApi.Services;

namespace Messages.WebApi.Middleware;

public sealed class LogContextMiddleware(RequestDelegate next)
{
    public const string CorrelationIdHeader = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 64)
            correlationId = context.TraceIdentifier;
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        var userId = context.User.FindFirst("sub")?.Value
                     ?? context.Request.Headers[HttpContextCurrentUser.UserIdHeader].FirstOrDefault();
        if (!Guid.TryParse(userId, out _))
            userId = "anonymous";

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("RequestPath", context.Request.Path.Value))
        {
            await next(context);
        }
    }
}
