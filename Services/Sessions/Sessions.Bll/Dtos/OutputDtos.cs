namespace Sessions.Bll.Dtos;

public class SkillDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class SessionSkillDto
{
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public int PlannedMinutes { get; set; }
    public DateTime AddedAt { get; set; }
}

public class SessionOutcomeDto
{
    public string? Summary { get; set; }
    public int ActualDurationMinutes { get; set; }
    public DateTime CompletedAt { get; set; }
}

public class SessionDto
{
    public int Id { get; set; }
    public int SwapId { get; set; }
    public byte FormatId { get; set; }
    public string FormatCode { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public string? Link { get; set; }
    public string? Location { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string RowVersion { get; set; } = string.Empty;
    public SessionOutcomeDto? Outcome { get; set; }
    public List<SessionSkillDto> Skills { get; set; } = [];
}

public class SessionListItemDto
{
    public int Id { get; set; }
    public int SwapId { get; set; }
    public string FormatCode { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string RowVersion { get; set; } = string.Empty;
}

public class SkillSessionDto
{
    public int SessionId { get; set; }
    public int SwapId { get; set; }
    public string FormatCode { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public int PlannedMinutes { get; set; }
    public DateTime AddedAt { get; set; }
}

public class AcceptedSwapSummaryDto
{
    public int SwapId { get; set; }
    public Guid InitiatorId { get; set; }
    public Guid PartnerId { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class AcceptedSwapDto : AcceptedSwapSummaryDto
{
    public List<SessionListItemDto> Sessions { get; set; } = [];
}

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}
