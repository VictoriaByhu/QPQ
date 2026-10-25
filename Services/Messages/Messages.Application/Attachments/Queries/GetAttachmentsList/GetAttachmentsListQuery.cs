using Messages.Application.Attachments.Dtos;
using Messages.Application.Common.Messaging;
using Messages.Application.Common.Models;

namespace Messages.Application.Attachments.Queries.GetAttachmentsList;

public sealed record GetAttachmentsListQuery(string ConversationId, string UserId, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<AttachmentDto>>;
