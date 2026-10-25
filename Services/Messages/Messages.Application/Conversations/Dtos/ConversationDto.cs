namespace Messages.Application.Conversations.Dtos;

public sealed record ConversationDto
{
    public string Id { get; init; } = string.Empty;
    public int SwapId { get; init; }
    public string SwapStatus { get; init; } = string.Empty;
    public IReadOnlyList<ParticipantDto> Participants { get; init; } = [];
    public LastMessageDto? LastMessage { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public int Version { get; init; }
}

public sealed record ParticipantDto
{
    public string UserId { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public DateTime? LastReadAt { get; init; }
}

public sealed record LastMessageDto
{
    public string MessageId { get; init; } = string.Empty;
    public string SenderId { get; init; } = string.Empty;
    public string Preview { get; init; } = string.Empty;
    public DateTime SentAt { get; init; }
}
