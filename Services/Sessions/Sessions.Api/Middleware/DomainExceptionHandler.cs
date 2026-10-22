using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Sessions.Domain.Exceptions;

namespace Sessions.Api.Middleware;

public sealed class DomainExceptionHandler(
    IProblemDetailsService problemDetails,
    IHostEnvironment environment,
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
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            BusinessConflictException => (StatusCodes.Status409Conflict, "Business rule conflict"),
            DomainException => (StatusCodes.Status409Conflict, "Domain rule violated"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error")
        };

        string detail;
        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                http.Request.Method, http.Request.Path);
            detail = environment.IsDevelopment() ? exception.ToString() : "An unexpected error occurred.";
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
