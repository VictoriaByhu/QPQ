using System.ComponentModel.DataAnnotations;

namespace UserSkills.Bll.Dtos;

public class LanguageInputDto
{
    /// <summary>Код мови з довідника, наприклад uk, en, pl.</summary>
    [Required, StringLength(10, MinimumLength = 1)]
    public string LanguageCode { get; set; } = "";

    /// <summary>Рівень володіння мовою: 1-5.</summary>
    [Range(1, 5)]
    public byte Proficiency { get; set; } = 3;
}

public class AvailabilitySlotInputDto
{
    /// <summary>1 = понеділок ... 7 = неділя.</summary>
    [Range(1, 7)]
    public byte DayOfWeek { get; set; }

    /// <summary>Початок у форматі HH:mm.</summary>
    [Required]
    public string StartTime { get; set; } = "";

    /// <summary>Кінець у форматі HH:mm (пізніше за початок).</summary>
    [Required]
    public string EndTime { get; set; } = "";
}

public class CreateUserSkillDto
{
    [Range(1, int.MaxValue)]
    public int SkillId { get; set; }

    /// <summary>Offer — пропоную навчати, Want — хочу вивчити.</summary>
    [Required, RegularExpression("^(Offer|Want)$", ErrorMessage = "Type must be Offer or Want.")]
    public string Type { get; set; } = "";

    [Range(1, 5)]
    public byte Level { get; set; } = 1;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(0, 80)]
    public byte? ExperienceYears { get; set; }

    [Url, StringLength(500)]
    public string? PortfolioUrl { get; set; }

    public List<LanguageInputDto> Languages { get; set; } = [];

    public List<AvailabilitySlotInputDto> Slots { get; set; } = [];
}

public class UpdateUserSkillDto
{
    /// <summary>RowVersion, отримана під час читання запису (base64).</summary>
    [Required]
    public byte[] RowVersion { get; set; } = [];

    [Range(1, 5)]
    public byte Level { get; set; } = 1;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(0, 80)]
    public byte? ExperienceYears { get; set; }

    [Url, StringLength(500)]
    public string? PortfolioUrl { get; set; }

    public List<LanguageInputDto> Languages { get; set; } = [];

    public List<AvailabilitySlotInputDto> Slots { get; set; } = [];
}

public class ChangeStatusDto
{
    [Required, RegularExpression("^(Active|Paused|Archived)$",
         ErrorMessage = "NewStatus must be Active, Paused or Archived.")]
    public string NewStatus { get; set; } = "";

    /// <summary>RowVersion, отримана під час читання запису (base64).</summary>
    [Required]
    public byte[] RowVersion { get; set; } = [];

    [StringLength(500)]
    public string? Comment { get; set; }
}
