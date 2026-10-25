using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Messages.Domain.Exceptions;

namespace Messages.WebApi.Middleware;

public sealed partial class ApiExceptionHandler(
    IProblemDetailsService problemDetails,
    IHostEnvironment environment,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken ct)
    {
        if (exception is OperationCanceledException && http.RequestAborted.IsCancellationRequested)
            return true;

        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            DomainException => (StatusCodes.Status400BadRequest, "Domain rule violated"),
            ConflictException => (StatusCodes.Status409Conflict, "Concurrency conflict"),
            MongoWriteException { WriteError.Category: ServerErrorCategory.DuplicateKey }
                => (StatusCodes.Status409Conflict, "Duplicate key"),
            MongoWriteException => (StatusCodes.Status400BadRequest, "Write rejected by MongoDB"),
            MongoConnectionException or TimeoutException
                => (StatusCodes.Status503ServiceUnavailable, "Database unavailable"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error")
        };

        var detail = DescribeAndLog(http, exception, status);

        var problem = exception is ValidationException validation
            ? new ValidationProblemDetails(validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray()))
            : new ProblemDetails();
        problem.Status = status;
        problem.Title = title;
        problem.Detail = detail;

        http.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = exception,
            ProblemDetails = problem
        });
    }

    private string DescribeAndLog(HttpContext http, Exception exception, int status)
    {
        switch (status)
        {
            case StatusCodes.Status500InternalServerError:
                logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                    http.Request.Method, http.Request.Path);
                return environment.IsDevelopment() ? exception.ToString() : "An unexpected error occurred.";

            case StatusCodes.Status503ServiceUnavailable:
                logger.LogError(exception, "MongoDB is unavailable for {Method} {Path}",
                    http.Request.Method, http.Request.Path);
                return "The database is temporarily unavailable. Try again later.";
        }

        logger.LogWarning("Request {Method} {Path} failed with {ExceptionType}: {Message}",
            http.Request.Method, http.Request.Path, exception.GetType().Name, exception.Message);

        return exception switch
        {
            ValidationException => "One or more validation errors occurred.",
            MongoWriteException { WriteError.Category: ServerErrorCategory.DuplicateKey } write
                => $"A document with the same unique key already exists (index {IndexName(write.WriteError.Message)}).",
            MongoWriteException write => environment.IsDevelopment()
                ? write.WriteError.Message
                : "The document was rejected by the database.",
            _ => exception.Message
        };
    }

    private static string IndexName(string message)
    {
        var match = IndexNameRegex().Match(message);
        return match.Success ? match.Groups[1].Value : "unknown";
    }

    [GeneratedRegex(@"index: (\S+)")]
    private static partial Regex IndexNameRegex();
}
