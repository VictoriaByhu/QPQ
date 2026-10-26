namespace Matching.Application.Common.Models;

// Вхідні елементи команд (рядки перевіряють валідатори, у доменні типи їх перетворює обробник).
public sealed record LanguageItem(string Code, int Proficiency);
public sealed record AvailabilityItem(int DayOfWeek, string Start, string End);

// Вихідні DTO: поля збігаються з документами MongoDB, enum-значення повертаються рядками.
public sealed record LanguageDto(string Code, int Proficiency);
public sealed record AvailabilityDto(int DayOfWeek, string Start, string End);

public sealed record IndexEntryDto(
    string Id, int UserSkillId, string UserId, int SkillId, string SkillName,
    string Type, int Level, string Status, int? ExperienceYears,
    IReadOnlyList<LanguageDto> Languages, IReadOnlyList<AvailabilityDto> Availability,
    int Version, DateTime? UpdatedAt);

public sealed record MatchOfferDto(string FromUserId, string ToUserId, int SkillId, string SkillName, string IndexRef);

public sealed record MatchDto(
    string Id, string Kind, string Status, int Score, IReadOnlyList<string> UserIds,
    MatchOfferDto Offer, MatchOfferDto? CounterOffer, IReadOnlyList<string> CommonLanguages,
    DateTime CreatedAt, DateTime ExpiresAt, int Version);

public sealed record LanguageCountDto(string Code, int Count);

public sealed record SkillStatsDto(
    int SkillId, string SkillName, int OffersCount, int WantsCount,
    IReadOnlyList<LanguageCountDto> TopLanguages, DateTime UpdatedAt);

public sealed record GenerateMatchesResult(int Created, int Upgraded, int Skipped);
