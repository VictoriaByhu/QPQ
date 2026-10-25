using MediatR;
using Microsoft.AspNetCore.Mvc;
using Messages.WebApi.Services;

namespace Messages.WebApi.Controllers;

[ApiController]
[Produces("application/json")]
[ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
public abstract class ApiControllerBase(ISender sender, ICurrentUser currentUser) : ControllerBase
{
    public const string IfMatchHeader = "If-Match";

    protected ISender Sender { get; } = sender;
    protected string UserId => currentUser.UserId;

    protected void SetETag(int version) => Response.Headers.ETag = $"\"{version}\"";

    protected static int? ParseVersion(string? ifMatch)
    {
        if (string.IsNullOrWhiteSpace(ifMatch) || ifMatch.Trim() == "*")
            return null;

        var value = ifMatch.Trim();
        if (value.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
            value = value[2..];
        value = value.Trim('"');

        return int.TryParse(value, out var version) && version >= 0 ? version : -1;
    }
}
