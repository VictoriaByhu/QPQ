using Microsoft.AspNetCore.Mvc;

namespace Matching.WebApi.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Версія документа з ETag/If-Match ("3" або W/"3"). Некоректне значення дає ProblemDetails 400,
    /// відсутній заголовок — 428 Precondition Required.
    /// </summary>
    protected bool TryReadIfMatch(string? ifMatch, out int version, out ActionResult? error)
    {
        version = 0;
        error = null;

        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            error = Problem(
                title: "Precondition required",
                detail: "Send the document version in the If-Match header (the ETag from GET).",
                statusCode: StatusCodes.Status428PreconditionRequired);
            return false;
        }

        var raw = ifMatch.Trim();
        if (raw.StartsWith("W/", StringComparison.OrdinalIgnoreCase)) raw = raw[2..];
        if (!int.TryParse(raw.Trim('"'), out version) || version < 0)
        {
            error = Problem(
                title: "Bad request",
                detail: "If-Match must contain the numeric document version, e.g. \"3\".",
                statusCode: StatusCodes.Status400BadRequest);
            return false;
        }
        return true;
    }

    protected void SetETag(int version) => Response.Headers.ETag = $"\"{version}\"";
}
