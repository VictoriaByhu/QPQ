using MediatR;
using Messages.Application.Conversations.Queries.GetSwapConversation;
using Messages.Application.Messages.Commands.SendFileMessage;
using Messages.Application.Messages.Commands.SendTextMessage;
using Messages.Domain.Constants;
using Platform.ServiceDefaults.Metrics;

namespace Messages.WebApi.Metrics;

/// <summary>
/// Pipeline step that measures the business operations listed below and counts sent messages.
/// Other requests pass through untouched; the handlers themselves are not changed.
/// </summary>
public sealed class MessageMetricsBehavior<TRequest, TResponse>(MessageMetrics metrics)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var (operation, sentType) = request switch
        {
            SendTextMessageCommand => ("send_text", MessageTypes.Text),
            SendFileMessageCommand => ("send_file", MessageTypes.File),
            GetSwapConversationQuery => ("get_swap_conversation", null),
            _ => (null, (string?)null)
        };

        if (operation is null)
            return await next(ct);

        var response = await metrics.Duration.MeasureAsync(operation, () => next(ct));
        if (sentType is not null)
            metrics.Sent(sentType);

        return response;
    }
}
