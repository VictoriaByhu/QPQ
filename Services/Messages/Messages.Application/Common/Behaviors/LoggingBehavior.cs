using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Messages.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName} {@Request}", requestName, request);

        try
        {
            var response = await next();
            logger.LogInformation("Handled {RequestName}", requestName);
            return response;
        }
        catch (ValidationException ex)
        {
            logger.LogWarning("{RequestName} rejected by validation, handler was not called: {Errors}",
                requestName, ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning("{RequestName} failed with {ExceptionType}: {Message}",
                requestName, ex.GetType().Name, ex.Message);
            throw;
        }
    }
}
