using Catalog.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FluentValidationException = FluentValidation.ValidationException;
using DomainValidationException = Catalog.Domain.Exceptions.ValidationException;

namespace Catalog.Api.Middleware;

/// <summary>
/// Єдина точка перетворення винятків на ProblemDetails (RFC 9457):
/// EntityNotFoundException -> 404, ValidationException -> 400, ConflictException і DomainException -> 409.
/// Кожен виняток логується один раз — тут.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken ct)
    {
        if (exception is OperationCanceledException && http.RequestAborted.IsCancellationRequested)
            return true;

        var (status, title) = exception switch
        {
            EntityNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            DomainValidationException or FluentValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            DomainException => (StatusCodes.Status409Conflict, "Business rule violated"),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Bad request"),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Request {Method} {Path} failed", http.Request.Method, http.Request.Path);
        else
            logger.LogWarning("Request {Method} {Path} failed with {ExceptionType}: {Message}",
                http.Request.Method, http.Request.Path, exception.GetType().Name, exception.Message);

        // Деталі внутрішньої помилки — лише в Development; клієнт у production бачить безпечний опис.
        var detail = status == StatusCodes.Status500InternalServerError
            ? (environment.IsDevelopment() ? exception.Message : "An unexpected error occurred.")
            : exception.Message;

        ProblemDetails body = exception switch
        {
            DomainValidationException v => new ValidationProblemDetails(
                v.Errors.ToDictionary(e => e.Key, e => e.Value)) { Status = status, Title = title },
            FluentValidationException f => new ValidationProblemDetails(
                f.Errors.GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
                { Status = status, Title = title },
            _ => new ProblemDetails { Status = status, Title = title, Detail = detail }
        };

        http.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = exception,
            ProblemDetails = body
        });
    }
}
