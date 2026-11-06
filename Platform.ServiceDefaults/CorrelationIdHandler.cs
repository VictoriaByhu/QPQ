using Microsoft.AspNetCore.Http;

namespace Platform.ServiceDefaults;

public sealed class CorrelationIdHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains(CorrelationIdMiddleware.HeaderName))
        {
            var correlationId = httpContextAccessor.HttpContext?.Request.Headers[CorrelationIdMiddleware.HeaderName].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(correlationId))
                request.Headers.TryAddWithoutValidation(CorrelationIdMiddleware.HeaderName, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
