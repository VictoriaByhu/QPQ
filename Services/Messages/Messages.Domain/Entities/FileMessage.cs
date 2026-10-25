using MongoDB.Bson;
using Messages.Domain.Common;
using Messages.Domain.Constants;
using Messages.Domain.Exceptions;
using Messages.Domain.ValueObjects;

namespace Messages.Domain.Entities;

public sealed class FileMessage : Message
{
    public const int CaptionMaxLength = 1000;
    public const int MaxAttachments = 10;

    private List<AttachmentRef> _attachments = [];

    public string? Caption { get; private set; }
    public IReadOnlyCollection<AttachmentRef> Attachments => _attachments.AsReadOnly();

    public override string Type => MessageTypes.File;
    public override string Preview => Caption ?? string.Join(", ", _attachments.Select(a => a.FileName));

    private FileMessage() { }

    public FileMessage(ObjectId conversationId, string senderId, string? caption, IEnumerable<AttachmentRef> attachments)
        : base(conversationId, senderId)
    {
        var items = attachments.ToList();
        if (items.Count == 0)
            throw new DomainException("A file message must contain at least one attachment.");
        if (items.Count > MaxAttachments)
            throw new DomainException($"A file message cannot contain more than {MaxAttachments} attachments.");
        if (items.Select(a => a.AttachmentId).Distinct().Count() != items.Count)
            throw new DomainException("Attachments must not repeat.");

        Caption = string.IsNullOrWhiteSpace(caption) ? null : Guard.Text(caption, nameof(Caption), CaptionMaxLength);
        _attachments.AddRange(items);
    }
}
