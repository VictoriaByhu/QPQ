using Messages.Application.Attachments.Dtos;
using Messages.Application.Common.Messaging;

namespace Messages.Application.Attachments.Commands.UploadAttachment;

public sealed record UploadAttachmentCommand(
    string ConversationId, string UserId, string? FileName, string? Url, string? ContentType, long SizeBytes)
    : ICommand<AttachmentDto>;
