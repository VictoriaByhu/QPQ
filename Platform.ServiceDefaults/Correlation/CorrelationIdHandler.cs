using Microsoft.AspNetCore.Http;

namespace Platform.ServiceDefaults.Correlation;

/// <summary>Додає CorrelationId поточного запиту у вихідні HTTP-виклики до інших сервісів.</summary>
public sealed class CorrelationIdHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var id = accessor.HttpContext?.Request.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        if (!string.IsNullOrEmpty(id))
            request.Headers.TryAddWithoutValidation(CorrelationIdMiddleware.HeaderName, id);

        return base.SendAsync(request, ct);
    }
}
