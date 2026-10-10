namespace Swaps.Bll.Dtos;

public class SkillDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class SwapSkillDto
{
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class SwapDetailsDto
{
    public string? Message { get; set; }
    public string? Location { get; set; }
    public DateTime? ProposedDate { get; set; }
    public int? DurationMinutes { get; set; }
}

public class SwapDto
{
    public int Id { get; set; }
    public Guid InitiatorId { get; set; }
    public Guid PartnerId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string RowVersion { get; set; } = string.Empty;
    public SwapDetailsDto? Details { get; set; }
    public List<SwapSkillDto> Skills { get; set; } = new();
}

public class SwapListItemDto
{
    public int Id { get; set; }
    public Guid InitiatorId { get; set; }
    public Guid PartnerId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public class SwapStatusHistoryDto
{
    public long Id { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public Guid ChangedByUserId { get; set; }
    public string? Comment { get; set; }
    public DateTime ChangedAt { get; set; }
}
