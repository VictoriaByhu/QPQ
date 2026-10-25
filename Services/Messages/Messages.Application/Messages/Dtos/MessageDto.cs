namespace Messages.Application.Messages.Dtos;

public sealed record MessageDto
{
    public string Id { get; init; } = string.Empty;
    public string ConversationId { get; init; } = string.Empty;
    public string SenderId { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string? Text { get; init; }
    public DateTime? EditedAt { get; init; }
    public string? Caption { get; init; }
    public IReadOnlyList<AttachmentRefDto> Attachments { get; init; } = [];
    public string? Event { get; init; }
    public int? SwapId { get; init; }
    public DateTime CreatedAt { get; init; }
    public int Version { get; init; }
}

public sealed record AttachmentRefDto
{
    public string AttachmentId { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
}
