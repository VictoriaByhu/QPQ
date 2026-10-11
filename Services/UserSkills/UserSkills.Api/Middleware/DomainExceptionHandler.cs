using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UserSkills.Domain.Exceptions;

namespace UserSkills.Api.Middleware;

/// <summary>
/// Єдина точка перетворення винятків на HTTP-статуси у форматі ProblemDetails (RFC 9457):
/// процедура (THROW) -> DAL (SqlExceptionTranslator) -> BLL (доменний виняток) -> цей обробник.
/// </summary>
public sealed class DomainExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext http, Exception exception, CancellationToken ct)
    {
        if (exception is OperationCanceledException && http.RequestAborted.IsCancellationRequested)
            return true;

        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            BusinessConflictException => (StatusCodes.Status409Conflict, "Business rule conflict"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error")
        };

        string detail;
        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                http.Request.Method, http.Request.Path);
            detail = "An unexpected error occurred."; // сирі SQL-повідомлення клієнту не віддаємо
        }
        else
        {
            logger.LogWarning("Request {Method} {Path} failed with {ExceptionType}: {Message}",
                http.Request.Method, http.Request.Path, exception.GetType().Name, exception.Message);
            detail = exception.Message;
        }

        http.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail
            }
        });
    }
}
