using System.Text.Json.Serialization;

namespace Platform.Aggregator.Models;

// Копії контрактів сервісів (сервіси не посилаються один на одного — лише контракт через JSON).
public sealed record SkillLevelDto(byte Level, string Title, string? Description);

public sealed record SkillDto(
    int Id, string Name, string Slug, int CategoryId, string CategoryName,
    string? Description, string? IconUrl,
    IReadOnlyList<SkillLevelDto> Levels, IReadOnlyList<string> Tags);

public sealed record UserSkillSummaryDto(
    int Id, Guid UserId, int SkillId, string SkillName, string Type, byte Level, string Status, DateTime CreatedAt);

public sealed record LanguageCountDto(string Language, int Count);

public sealed record SkillStatsDto(
    int SkillId, string SkillName, int OffersCount, int WantsCount,
    IReadOnlyList<LanguageCountDto> TopLanguages, DateTime UpdatedAt);

/// <summary>
/// Картка навички — бізнес-сценарій, а не копія структур сервісів:
/// довідник (catalog-api) + хто навчає/хоче вчитись (userskills-api) + статистика пошуку пар (matching-api).
/// </summary>
public sealed record FullSkillDto(
    SkillDto Skill,
    IReadOnlyList<UserSkillSummaryDto> UserSkills,
    SkillStatsDto? Stats,
    IReadOnlyList<string> Warnings);
