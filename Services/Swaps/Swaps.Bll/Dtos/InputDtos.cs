using System.ComponentModel.DataAnnotations;

namespace Swaps.Bll.Dtos;

public class CreateSwapDto
{
    public Guid PartnerId { get; set; }

    [Range(1, int.MaxValue)]
    public int OfferedSkillId { get; set; }

    [Range(1, int.MaxValue)]
    public int RequestedSkillId { get; set; }

    [StringLength(1000)]
    public string? Message { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }

    public DateTime? ProposedDate { get; set; }

    [Range(1, 1440)]
    public int? DurationMinutes { get; set; }
}

public class UpdateSwapDetailsDto
{
    [StringLength(1000)]
    public string? Message { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }

    public DateTime? ProposedDate { get; set; }

    [Range(1, 1440)]
    public int? DurationMinutes { get; set; }

    [StringLength(24)]
    public string? RowVersion { get; set; }
}

public class ChangeSwapStatusDto
{
    [StringLength(24)]
    public string? RowVersion { get; set; }

    [StringLength(500)]
    public string? Comment { get; set; }
}

public class SwapQueryDto
{
    public string? Role { get; set; }

    public string? Status { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
