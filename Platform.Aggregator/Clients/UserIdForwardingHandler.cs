namespace Platform.Aggregator.Clients;

public sealed class UserIdForwardingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    public const string HeaderName = "X-User-Id";
    public const string ParticipantItemKey = "Aggregator.ParticipantUserId";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext;
        var userId = context?.Items[ParticipantItemKey] as string
                     ?? context?.Request.Headers[HeaderName].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(userId) && !request.Headers.Contains(HeaderName))
            request.Headers.TryAddWithoutValidation(HeaderName, userId);

        return base.SendAsync(request, cancellationToken);
    }
}
