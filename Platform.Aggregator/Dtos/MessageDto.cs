namespace Platform.Aggregator.Dtos;

public record MessageDto(
    string Id,
    string ConversationId,
    string SenderId,
    string Type,
    string? Text,
    string? Caption,
    string? Event,
    DateTime CreatedAt,
    DateTime? EditedAt);

public record ConversationDto(string Id, int SwapId, string SwapStatus);

public record PagedResult<T>(IReadOnlyList<T> Items, long TotalCount, int Page, int PageSize, int TotalPages);

public record ConversationMessagesDto(ConversationDto? Conversation, IReadOnlyList<MessageDto> Messages)
{
    public static ConversationMessagesDto Empty { get; } = new(null, []);
}
