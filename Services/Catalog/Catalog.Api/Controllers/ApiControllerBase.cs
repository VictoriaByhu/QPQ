using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Явний виклик FluentValidation (автоматична інтеграція застаріла).
    /// Повертає ValidationProblemDetails (400) або null, якщо дані коректні.
    /// </summary>
    protected async Task<ActionResult?> ValidateAsync<T>(IValidator<T> validator, T input, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(input, ct);
        return result.IsValid ? null : ValidationProblem(new ValidationProblemDetails(result.ToDictionary()));
    }

    /// <summary>Метадані пагінації дублюються в заголовках, щоб клієнт і кеш могли їх читати без тіла.</summary>
    protected void AddPagingHeaders(int totalCount, int page, int pageSize, int totalPages)
    {
        Response.Headers["X-Total-Count"] = totalCount.ToString();
        Response.Headers["X-Page"] = page.ToString();
        Response.Headers["X-Page-Size"] = pageSize.ToString();
        Response.Headers["X-Total-Pages"] = totalPages.ToString();
    }
}
