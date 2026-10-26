using MediatR;
using Microsoft.Extensions.Logging;

namespace Matching.Application.Common.Behaviors;

/// <summary>
/// Структуроване логування всіх запитів і відповідей (Serilog). Зовнішній behavior у pipeline:
/// якщо ValidationBehavior відхилив запит, у лозі буде "Handling", але не "Handled" — обробник не виконувався.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName} {@Request}", name, request);
        try
        {
            var response = await next(ct);
            logger.LogInformation("Handled {RequestName}", name);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Request {RequestName} failed before completion: {ExceptionType}",
                name, ex.GetType().Name);
            throw;
        }
    }
}
