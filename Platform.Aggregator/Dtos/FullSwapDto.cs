namespace Platform.Aggregator.Dtos;

public record FullSwapDto(
    SwapDto Swap,
    IReadOnlyList<SessionDto> Sessions,
    IReadOnlyList<MessageDto> Messages,
    IReadOnlyList<string> Warnings);
