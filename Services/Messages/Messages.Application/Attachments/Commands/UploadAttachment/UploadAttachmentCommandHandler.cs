using AutoMapper;
using MediatR;
using Messages.Application.Attachments.Dtos;
using Messages.Application.Common;
using Messages.Domain.Entities;
using Messages.Domain.Interfaces;

namespace Messages.Application.Attachments.Commands.UploadAttachment;

public sealed class UploadAttachmentCommandHandler(
    IConversationRepository conversations,
    IAttachmentRepository attachments,
    IMapper mapper) : IRequestHandler<UploadAttachmentCommand, AttachmentDto>
{
    public async Task<AttachmentDto> Handle(UploadAttachmentCommand command, CancellationToken ct)
    {
        var conversation = await conversations.GetForParticipantAsync(command.ConversationId, command.UserId, ct);
        conversation.EnsureAcceptsContent();

        var attachment = new Attachment(
            conversation.Id, command.UserId, command.FileName!, command.Url!, command.ContentType!, command.SizeBytes);
        await attachments.AddAsync(attachment, ct);

        return mapper.Map<AttachmentDto>(attachment);
    }
}
