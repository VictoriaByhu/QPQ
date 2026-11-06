namespace Platform.Aggregator.Dtos;

public record SwapDto(
    int Id,
    Guid InitiatorId,
    Guid PartnerId,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    SwapDetailsDto? Details,
    IReadOnlyList<SwapSkillDto> Skills);

public record SwapDetailsDto(string? Message, string? Location, DateTime? ProposedDate, int? DurationMinutes);

public record SwapSkillDto(int SkillId, string SkillName, string Role);
