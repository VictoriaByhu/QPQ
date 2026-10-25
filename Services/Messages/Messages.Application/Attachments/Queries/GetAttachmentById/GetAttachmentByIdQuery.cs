using Messages.Application.Attachments.Dtos;
using Messages.Application.Common.Messaging;

namespace Messages.Application.Attachments.Queries.GetAttachmentById;

public sealed record GetAttachmentByIdQuery(string Id, string UserId) : IQuery<AttachmentDto>;
