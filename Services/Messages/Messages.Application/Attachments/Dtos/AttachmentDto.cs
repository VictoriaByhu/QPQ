namespace Messages.Application.Attachments.Dtos;

public sealed record AttachmentDto
{
    public string Id { get; init; } = string.Empty;
    public string ConversationId { get; init; } = string.Empty;
    public string UploadedBy { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public DateTime UploadedAt { get; init; }
}
