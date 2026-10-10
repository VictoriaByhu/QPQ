using System.Security.Claims;

namespace Swaps.Api.Services;

public interface ICurrentUser
{
    Guid UserId { get; }
}

public sealed class HttpContextCurrentUser(IHttpContextAccessor accessor, IWebHostEnvironment environment)
    : ICurrentUser
{
    private const string UserIdHeader = "X-User-Id";

    public Guid UserId
    {
        get
        {
            var context = accessor.HttpContext
                          ?? throw new UnauthorizedAccessException("No active HTTP context.");

            var value = context.User.FindFirst("sub")?.Value
                        ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (value is null && environment.IsDevelopment())
                value = context.Request.Headers[UserIdHeader].FirstOrDefault();

            if (Guid.TryParse(value, out var userId) && userId != Guid.Empty)
                return userId;

            throw new UnauthorizedAccessException(
                "User identity is missing. Provide a token with the 'sub' claim or, in Development, the X-User-Id header.");
        }
    }
}
