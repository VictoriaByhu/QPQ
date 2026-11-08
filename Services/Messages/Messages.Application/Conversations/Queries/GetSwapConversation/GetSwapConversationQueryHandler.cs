using MediatR;
using Messages.Application.Common.Interfaces;
using Messages.Application.Common.Validation;
using Messages.Application.Conversations.Dtos;
using Messages.Application.Messages.Dtos;
using Messages.Domain.Exceptions;

namespace Messages.Application.Conversations.Queries.GetSwapConversation;

public sealed class GetSwapConversationQueryHandler(IMessageQueries queries)
    : IRequestHandler<GetSwapConversationQuery, SwapConversationDto>
{
    public async Task<SwapConversationDto> Handle(GetSwapConversationQuery query, CancellationToken ct)
    {
        var conversation = await queries.GetConversationBySwapAsync(query.SwapId, query.UserId, ct)
                           ?? throw new NotFoundException($"Conversation of swap {query.SwapId} was not found.");

        var messages = new List<MessageDto>();
        for (var page = 1; ; page++)
        {
            var result = await queries.GetMessagesAsync(
                conversation.Id, null, null, page, RuleBuilderExtensions.MaxPageSize, ct);

            messages.AddRange(result.Items);
            if (!result.HasNext)
                break;
        }

        return new SwapConversationDto(conversation, messages);
    }
}
