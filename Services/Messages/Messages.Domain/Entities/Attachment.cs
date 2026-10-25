using MongoDB.Bson;
using Messages.Domain.Common;
using Messages.Domain.Exceptions;
using Messages.Domain.ValueObjects;

namespace Messages.Domain.Entities;

public sealed class Attachment : BaseEntity
{
    public const int UrlMaxLength = 500;
    public const int ContentTypeMaxLength = 100;
    public const long MaxSizeBytes = 50L * 1024 * 1024;

    public ObjectId ConversationId { get; private set; }
    public string UploadedBy { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }

    public DateTime UploadedAt
    {
        get => CreatedAt;
        private set => CreatedAt = value;
    }

    private Attachment() { }

    public Attachment(
        ObjectId conversationId, string uploadedBy, string fileName,
        string url, string contentType, long sizeBytes)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new DomainException("Url must be an absolute http(s) address.");
        if (url.Length > UrlMaxLength)
            throw new DomainException($"Url must not exceed {UrlMaxLength} characters.");
        if (sizeBytes is < 0 or > MaxSizeBytes)
            throw new DomainException($"File size must be between 0 and {MaxSizeBytes} bytes.");

        ConversationId = Guard.Id(conversationId, nameof(ConversationId));
        UploadedBy = Guard.UserId(uploadedBy, nameof(UploadedBy));
        FileName = Guard.Text(fileName, nameof(FileName), AttachmentRef.FileNameMaxLength);
        ContentType = Guard.Text(contentType, nameof(ContentType), ContentTypeMaxLength);
        Url = url;
        SizeBytes = sizeBytes;
    }

    public AttachmentRef ToRef() => new(Id, FileName);
}
