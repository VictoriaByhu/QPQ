using MediatR;
using MongoDB.Bson;
using Messages.Application.Attachments.Dtos;
using Messages.Application.Common.Interfaces;
using Messages.Domain.Exceptions;
using Messages.Domain.Interfaces;

namespace Messages.Application.Attachments.Queries.GetAttachmentById;

public sealed class GetAttachmentByIdQueryHandler(IMessageQueries queries, IConversationRepository conversations)
    : IRequestHandler<GetAttachmentByIdQuery, AttachmentDto>
{
    public async Task<AttachmentDto> Handle(GetAttachmentByIdQuery query, CancellationToken ct)
    {
        var attachment = await queries.GetAttachmentAsync(query.Id, ct);
        if (attachment is null
            || !await conversations.IsParticipantAsync(ObjectId.Parse(attachment.ConversationId), query.UserId, ct))
            throw new NotFoundException($"Attachment {query.Id} was not found.");
        return attachment;
    }
}
