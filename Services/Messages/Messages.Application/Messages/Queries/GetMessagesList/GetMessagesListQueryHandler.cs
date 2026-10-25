using MediatR;
using Messages.Application.Common;
using Messages.Application.Common.Interfaces;
using Messages.Application.Common.Models;
using Messages.Application.Messages.Dtos;
using Messages.Domain.Interfaces;

namespace Messages.Application.Messages.Queries.GetMessagesList;

public sealed class GetMessagesListQueryHandler(IMessageQueries queries, IConversationRepository conversations)
    : IRequestHandler<GetMessagesListQuery, PagedResult<MessageDto>>
{
    public async Task<PagedResult<MessageDto>> Handle(GetMessagesListQuery query, CancellationToken ct)
    {
        await conversations.EnsureParticipantAsync(query.ConversationId, query.UserId, ct);

        var senderId = query.SenderId is null ? null : Guid.Parse(query.SenderId).ToString("D");
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        return await queries.GetMessagesAsync(query.ConversationId, search, senderId, query.Page, query.PageSize, ct);
    }
}
