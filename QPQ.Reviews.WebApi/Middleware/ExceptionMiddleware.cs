using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using QPQ.Reviews.Domain.Exceptions;

using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace QPQ.Reviews.WebApi.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Instance = context.Request.Path
        };

        switch (exception)
        {
            case NotFoundException notFoundEx:
                problemDetails.Title = "Not Found";
                problemDetails.Status = StatusCodes.Status404NotFound;
                problemDetails.Detail = notFoundEx.Message;
                break;

            case ValidationException validationEx:
                problemDetails.Title = "Validation Error";
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Detail = "One or more validation errors occurred.";
                problemDetails.Extensions["errors"] = validationEx.Errors;
                break;

            case ConflictException conflictEx:
                problemDetails.Title = "Conflict";
                problemDetails.Status = StatusCodes.Status409Conflict;
                problemDetails.Detail = conflictEx.Message;
                break;

            case MongoWriteException mongoEx when mongoEx.WriteError.Category == ServerErrorCategory.DuplicateKey:
                problemDetails.Title = "Duplicate Key Conflict";
                problemDetails.Status = StatusCodes.Status409Conflict;
                problemDetails.Detail = "A document with the same unique key already exists.";
                break;

            case MongoConnectionException:
                problemDetails.Title = "Database Connection Error";
                problemDetails.Status = StatusCodes.Status503ServiceUnavailable;
                problemDetails.Detail = "Unable to connect to the database.";
                break;

            default:
                problemDetails.Title = "Internal Server Error";
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Detail = exception.Message;
                break;
        }

        context.Response.StatusCode = problemDetails.Status.Value;
        var result = JsonSerializer.Serialize(problemDetails);
        await context.Response.WriteAsync(result);
    }
}