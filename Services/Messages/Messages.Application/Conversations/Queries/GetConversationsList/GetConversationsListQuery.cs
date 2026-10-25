using Messages.Application.Common.Messaging;
using Messages.Application.Common.Models;
using Messages.Application.Conversations.Dtos;

namespace Messages.Application.Conversations.Queries.GetConversationsList;

public sealed record GetConversationsListQuery(string UserId, string? SwapStatus, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<ConversationDto>>;
