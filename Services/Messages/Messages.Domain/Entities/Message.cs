using MongoDB.Bson;
using Messages.Domain.Common;
using Messages.Domain.Exceptions;

namespace Messages.Domain.Entities;

public abstract class Message : BaseEntity
{
    public ObjectId ConversationId { get; private set; }
    public string SenderId { get; private set; } = string.Empty;
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public abstract string Type { get; }
    public abstract string Preview { get; }

    protected Message() { }

    protected Message(ObjectId conversationId, string senderId)
    {
        ConversationId = Guard.Id(conversationId, nameof(ConversationId));
        SenderId = Guard.UserId(senderId, nameof(SenderId));
    }

    public bool IsSentBy(string userId)
        => Guid.TryParse(userId, out var id) && SenderId == id.ToString("D");

    public virtual void Delete(string userId)
    {
        EnsureChangeableBy(userId);
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        Touch();
    }

    protected void EnsureChangeableBy(string userId)
    {
        if (IsDeleted)
            throw new DomainException("The message has been deleted.");
        if (!IsSentBy(userId))
            throw new DomainException("Only the sender can change the message.");
    }
}
