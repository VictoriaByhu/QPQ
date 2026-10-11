namespace UserSkills.Domain.Models;

/// <summary>Навичка користувача: пропоную (Offer) або хочу вивчити (Want).</summary>
public class UserSkill
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public int SkillId { get; set; }

    /// <summary>Назва навички з локальної копії довідника (заповнюється процедурами читання).</summary>
    public string SkillName { get; set; } = "";

    public string Type { get; set; } = "";
    public byte Level { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Токен оптимістичної конкурентності (ROWVERSION).</summary>
    public byte[] RowVersion { get; set; } = [];

    public UserSkillDetails? Details { get; set; }
    public List<UserSkillLanguage> Languages { get; set; } = [];
    public List<AvailabilitySlot> Slots { get; set; } = [];
}

public class UserSkillDetails
{
    public int UserSkillId { get; set; }
    public string? Description { get; set; }
    public byte? ExperienceYears { get; set; }
    public string? PortfolioUrl { get; set; }
}

public class UserSkillLanguage
{
    public string LanguageCode { get; set; } = "";
    public string LanguageName { get; set; } = "";
    public byte Proficiency { get; set; }
}

public class AvailabilitySlot
{
    /// <summary>1 = понеділок ... 7 = неділя.</summary>
    public byte DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

public class UserSkillStatusHistoryEntry
{
    public long Id { get; set; }
    public int UserSkillId { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = "";
    public Guid ChangedByUserId { get; set; }
    public string? Comment { get; set; }
    public DateTime ChangedAt { get; set; }
}
