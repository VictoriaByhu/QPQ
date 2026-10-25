using MongoDB.Bson;
using Messages.Domain.Common;
using Messages.Domain.Entities;
using Messages.Domain.Exceptions;
using Messages.Domain.Interfaces;

namespace Messages.Application.Common;

public static class EntityGuards
{
    public static async Task<Conversation> GetForParticipantAsync(
        this IConversationRepository conversations, string conversationId, string userId, CancellationToken ct)
    {
        var conversation = await conversations.GetByIdAsync(ObjectId.Parse(conversationId), ct);
        if (conversation is null || !conversation.HasParticipant(userId))
            throw new NotFoundException($"Conversation {conversationId} was not found.");
        return conversation;
    }

    public static async Task<Message> GetVisibleAsync(
        this IMessageRepository messages, IConversationRepository conversations,
        string messageId, string userId, CancellationToken ct)
    {
        var message = await messages.GetByIdAsync(ObjectId.Parse(messageId), ct);
        if (message is null || message.IsDeleted
            || !await conversations.IsParticipantAsync(message.ConversationId, userId, ct))
            throw new NotFoundException($"Message {messageId} was not found.");
        return message;
    }

    public static async Task EnsureParticipantAsync(
        this IConversationRepository conversations, string conversationId, string userId, CancellationToken ct)
    {
        if (!await conversations.IsParticipantAsync(ObjectId.Parse(conversationId), userId, ct))
            throw new NotFoundException($"Conversation {conversationId} was not found.");
    }

    public static void EnsureVersion(this BaseEntity entity, int? expectedVersion)
    {
        if (expectedVersion is { } expected && expected != entity.Version)
            throw new ConflictException(
                $"Version conflict: expected {expected}, but the current version is {entity.Version}.");
    }
}
