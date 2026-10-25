using MongoDB.Bson;
using Messages.Domain.Common;
using Messages.Domain.Exceptions;

namespace Messages.Domain.ValueObjects;

public sealed class LastMessage : ValueObject
{
    public const int PreviewMaxLength = 200;

    public ObjectId MessageId { get; }
    public string SenderId { get; }
    public string Preview { get; }
    public DateTime SentAt { get; }

    public LastMessage(ObjectId messageId, string senderId, string preview, DateTime sentAt)
    {
        if (preview is null)
            throw new DomainException("Preview is required.");
        if (preview.Length > PreviewMaxLength)
            throw new DomainException($"Preview must not exceed {PreviewMaxLength} characters.");

        MessageId = Guard.Id(messageId, nameof(MessageId));
        SenderId = Guard.UserId(senderId, nameof(SenderId));
        Preview = preview;
        SentAt = sentAt;
    }

    public static string Shorten(string text)
        => text.Length <= PreviewMaxLength ? text : string.Concat(text.AsSpan(0, PreviewMaxLength - 3), "...");

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MessageId;
        yield return SenderId;
        yield return Preview;
        yield return SentAt;
    }
}
