using MediatR;
using Messages.Application.Attachments.Dtos;
using Messages.Application.Common;
using Messages.Application.Common.Interfaces;
using Messages.Application.Common.Models;
using Messages.Domain.Interfaces;

namespace Messages.Application.Attachments.Queries.GetAttachmentsList;

public sealed class GetAttachmentsListQueryHandler(IMessageQueries queries, IConversationRepository conversations)
    : IRequestHandler<GetAttachmentsListQuery, PagedResult<AttachmentDto>>
{
    public async Task<PagedResult<AttachmentDto>> Handle(GetAttachmentsListQuery query, CancellationToken ct)
    {
        await conversations.EnsureParticipantAsync(query.ConversationId, query.UserId, ct);
        return await queries.GetAttachmentsAsync(query.ConversationId, query.Page, query.PageSize, ct);
    }
}
