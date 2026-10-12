using System.ComponentModel.DataAnnotations;

namespace QPQ.Moderation.Bll.DTOs;

public class ChangeStatusDto
{
    [Required]
    [MinLength(3)]
    public string NewStatus { get; set; } = string.Empty;
}