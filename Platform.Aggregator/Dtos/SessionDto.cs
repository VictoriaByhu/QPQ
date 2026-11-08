namespace Platform.Aggregator.Dtos;

public record SessionDto(
    int Id,
    int SwapId,
    string FormatCode,
    DateTime ScheduledAt,
    int DurationMinutes,
    string Status);
