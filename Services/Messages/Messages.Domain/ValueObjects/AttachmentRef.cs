using MongoDB.Bson;
using Messages.Domain.Common;

namespace Messages.Domain.ValueObjects;

public sealed class AttachmentRef : ValueObject
{
    public const int FileNameMaxLength = 255;

    public ObjectId AttachmentId { get; }
    public string FileName { get; }

    public AttachmentRef(ObjectId attachmentId, string fileName)
    {
        AttachmentId = Guard.Id(attachmentId, nameof(AttachmentId));
        FileName = Guard.Text(fileName, nameof(FileName), FileNameMaxLength);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AttachmentId;
        yield return FileName;
    }
}
