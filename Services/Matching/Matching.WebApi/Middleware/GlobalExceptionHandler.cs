using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Matching.Application.Common.Exceptions;
using Matching.Domain.Exceptions;

namespace Matching.WebApi.Middleware;

/// <summary>
/// Єдина точка перетворення винятків на ProblemDetails (RFC 9457), включно з MongoDB-специфічними.
/// Кожен виняток логується один раз — тут.
/// NotFound -> 404; Validation, Domain -> 400; Conflict, DuplicateKey -> 409;
/// інші помилки запису -> 400; недоступний сервер MongoDB -> 503.
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

        var (status, title) = Classify(exception);

        if (status >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Request {Method} {Path} failed with {Status}",
                http.Request.Method, http.Request.Path, status);
        else
            logger.LogWarning("Request {Method} {Path} failed with {ExceptionType}: {Message}",
                http.Request.Method, http.Request.Path, exception.GetType().Name, exception.Message);

        // Внутрішні деталі (рядки підключення, тексти драйвера) показуємо лише в Development.
        var detail = status switch
        {
            StatusCodes.Status503ServiceUnavailable => "The database is temporarily unavailable. Try again later.",
            StatusCodes.Status500InternalServerError => environment.IsDevelopment() ? exception.Message : "An unexpected error occurred.",
            _ when exception is MongoWriteException or MongoBulkWriteException => ClientMessage(exception),
            _ => exception.Message
        };

        ProblemDetails body = exception switch
        {
            ValidationException v => new ValidationProblemDetails(
                v.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray()))
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

    private static (int Status, string Title) Classify(Exception ex) => ex switch
    {
        NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
        ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
        DomainException => (StatusCodes.Status400BadRequest, "Business rule violated"),
        ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
        MongoWriteException { WriteError.Category: ServerErrorCategory.DuplicateKey }
            => (StatusCodes.Status409Conflict, "Duplicate document"),
        MongoBulkWriteException bulk when bulk.WriteErrors.Any(e => e.Category == ServerErrorCategory.DuplicateKey)
            => (StatusCodes.Status409Conflict, "Duplicate document"),
        MongoWriteException or MongoBulkWriteException => (StatusCodes.Status400BadRequest, "Write rejected"),
        // 121 = DocumentValidationFailure: документ не пройшов $jsonSchema колекції.
        MongoCommandException { Code: 121 } => (StatusCodes.Status400BadRequest, "Document failed schema validation"),
        MongoConnectionException or MongoAuthenticationException or TimeoutException
            => (StatusCodes.Status503ServiceUnavailable, "Service unavailable"),
        BadHttpRequestException => (StatusCodes.Status400BadRequest, "Bad request"),
        _ => (StatusCodes.Status500InternalServerError, "Internal server error")
    };

    private static string ClientMessage(Exception ex) => ex is MongoWriteException { WriteError.Category: ServerErrorCategory.DuplicateKey }
        or MongoBulkWriteException
        ? "A document with the same unique key already exists."
        : "The database rejected the write.";
}
