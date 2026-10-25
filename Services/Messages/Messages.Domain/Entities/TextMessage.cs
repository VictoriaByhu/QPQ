using MongoDB.Bson;
using Messages.Domain.Common;
using Messages.Domain.Constants;
using Messages.Domain.Exceptions;

namespace Messages.Domain.Entities;

public sealed class TextMessage : Message
{
    public const int TextMaxLength = 4000;

    public string Text { get; private set; } = string.Empty;
    public DateTime? EditedAt { get; private set; }

    public override string Type => MessageTypes.Text;
    public override string Preview => Text;

    private TextMessage() { }

    public TextMessage(ObjectId conversationId, string senderId, string text)
        : base(conversationId, senderId)
    {
        Text = Guard.Text(text, nameof(Text), TextMaxLength);
    }

    public void Edit(string userId, string text)
    {
        EnsureChangeableBy(userId);
        var newText = Guard.Text(text, nameof(Text), TextMaxLength);
        if (newText == Text)
            throw new DomainException("The new text is the same as the current one.");

        Text = newText;
        EditedAt = DateTime.UtcNow;
        Touch();
    }
}
