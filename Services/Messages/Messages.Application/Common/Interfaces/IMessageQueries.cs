using Messages.Application.Attachments.Dtos;
using Messages.Application.Common.Models;
using Messages.Application.Conversations.Dtos;
using Messages.Application.Messages.Dtos;

namespace Messages.Application.Common.Interfaces;

public interface IMessageQueries
{
    Task<ConversationDto?> GetConversationAsync(string id, string userId, CancellationToken ct);

    Task<PagedResult<ConversationDto>> GetConversationsAsync(
        string userId, string? swapStatus, int page, int pageSize, CancellationToken ct);

    Task<MessageDto?> GetMessageAsync(string id, CancellationToken ct);

    Task<PagedResult<MessageDto>> GetMessagesAsync(
        string conversationId, string? search, string? senderId, int page, int pageSize, CancellationToken ct);

    Task<AttachmentDto?> GetAttachmentAsync(string id, CancellationToken ct);

    Task<PagedResult<AttachmentDto>> GetAttachmentsAsync(
        string conversationId, int page, int pageSize, CancellationToken ct);
}
