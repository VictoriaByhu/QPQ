using MediatR;
using Messages.Application.Common.Interfaces;
using Messages.Application.Common.Models;
using Messages.Application.Conversations.Dtos;
using Messages.Domain.Enums;

namespace Messages.Application.Conversations.Queries.GetConversationsList;

public sealed class GetConversationsListQueryHandler(IMessageQueries queries)
    : IRequestHandler<GetConversationsListQuery, PagedResult<ConversationDto>>
{
    public Task<PagedResult<ConversationDto>> Handle(GetConversationsListQuery query, CancellationToken ct)
    {
        var status = query.SwapStatus is null ? null : Enum.Parse<SwapStatus>(query.SwapStatus, true).ToString();
        return queries.GetConversationsAsync(query.UserId, status, query.Page, query.PageSize, ct);
    }
}
