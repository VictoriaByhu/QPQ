using Messages.Application.Common.Messaging;
using Messages.Application.Common.Models;
using Messages.Application.Messages.Dtos;

namespace Messages.Application.Messages.Queries.GetMessagesList;

public sealed record GetMessagesListQuery(
    string ConversationId, string UserId, string? Search, string? SenderId, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<MessageDto>>;
