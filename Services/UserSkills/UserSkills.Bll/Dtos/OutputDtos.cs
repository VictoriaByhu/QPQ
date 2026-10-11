namespace UserSkills.Bll.Dtos;

public record LanguageDto(string LanguageCode, string LanguageName, byte Proficiency);

public record AvailabilitySlotDto(byte DayOfWeek, string StartTime, string EndTime);

public record UserSkillDetailsDto(string? Description, byte? ExperienceYears, string? PortfolioUrl);

/// <summary>Короткий рядок списку навичок користувача.</summary>
public record UserSkillSummaryDto(
    int Id, Guid UserId, int SkillId, string SkillName, string Type, byte Level,
    string Status, DateTime CreatedAt, byte[] RowVersion);

/// <summary>Повний запис з деталями, мовами та доступністю.</summary>
public record UserSkillDto(
    int Id, Guid UserId, int SkillId, string SkillName, string Type, byte Level,
    string Status, DateTime CreatedAt, DateTime? UpdatedAt, byte[] RowVersion,
    UserSkillDetailsDto? Details,
    IReadOnlyList<LanguageDto> Languages,
    IReadOnlyList<AvailabilitySlotDto> Slots);

public record StatusHistoryDto(
    long Id, int UserSkillId, string? OldStatus, string NewStatus,
    Guid ChangedByUserId, string? Comment, DateTime ChangedAt);

/// <summary>Нова RowVersion після зміни.</summary>
public record RowVersionDto(byte[] RowVersion);

public record SkillDto(int Id, string Name);

public record LanguageRefDto(string Code, string Name);
