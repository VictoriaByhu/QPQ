using System.ComponentModel.DataAnnotations;

namespace Sessions.Bll.Dtos;

public interface ISessionPlaceInput
{
    byte FormatId { get; }
    DateTime? ScheduledAt { get; }
    int DurationMinutes { get; }
    string? Link { get; }
    string? Location { get; }
}

public class SessionSkillInputDto
{
    [Range(1, int.MaxValue)]
    public int SkillId { get; set; }

    [Range(1, 1440)]
    public int PlannedMinutes { get; set; } = 30;
}

public class CreateSessionDto : ISessionPlaceInput
{
    [Range(1, int.MaxValue)]
    public int SwapId { get; set; }

    [Range(1, byte.MaxValue)]
    public byte FormatId { get; set; }

    [Required]
    public DateTime? ScheduledAt { get; set; }

    [Range(1, 1440)]
    public int DurationMinutes { get; set; } = 60;

    [StringLength(500)]
    public string? Link { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }

    public List<SessionSkillInputDto> Skills { get; set; } = [];
}

public class UpdateSessionDto : ISessionPlaceInput
{
    [Range(1, byte.MaxValue)]
    public byte FormatId { get; set; }

    [Required]
    public DateTime? ScheduledAt { get; set; }

    [Range(1, 1440)]
    public int DurationMinutes { get; set; }

    [StringLength(500)]
    public string? Link { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }

    [StringLength(24)]
    public string? RowVersion { get; set; }
}

public class CompleteSessionDto
{
    [StringLength(1000)]
    public string? Summary { get; set; }

    [Range(1, 1440)]
    public int ActualDurationMinutes { get; set; }

    [StringLength(24)]
    public string? RowVersion { get; set; }
}

public class ChangeSessionStatusDto
{
    [StringLength(24)]
    public string? RowVersion { get; set; }
}

public class AddSessionSkillDto : SessionSkillInputDto
{
    [StringLength(24)]
    public string? RowVersion { get; set; }
}

public class SessionQueryDto
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;

    [Range(1, int.MaxValue)]
    public int? SwapId { get; set; }

    [StringLength(20)]
    public string? Status { get; set; }

    [Range(1, byte.MaxValue)]
    public byte? FormatId { get; set; }

    [Range(1, int.MaxValue)]
    public int? SkillId { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    [StringLength(100)]
    public string? Search { get; set; }

    public string SortBy { get; set; } = "scheduledAt";

    public string SortDir { get; set; } = "asc";
}
