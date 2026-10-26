using System.ComponentModel.DataAnnotations;

namespace Matching.WebApi.Contracts;

public sealed record LanguageRequest(string Code, int Proficiency);
public sealed record AvailabilityRequest(int DayOfWeek, string Start, string End);

/// <summary>Тіло POST /api/index-entries.</summary>
public sealed record CreateIndexEntryRequest(
    int UserSkillId, string UserId, int SkillId, string SkillName,
    string Type, int Level, string Status, int? ExperienceYears,
    List<LanguageRequest>? Languages, List<AvailabilityRequest>? Availability);

/// <summary>Тіло PUT /api/index-entries/{id}. Версію документа передає заголовок If-Match.</summary>
public sealed record UpdateIndexEntryRequest(
    string SkillName, int Level, string Status, int? ExperienceYears,
    List<LanguageRequest>? Languages, List<AvailabilityRequest>? Availability);

/// <summary>Тіло PUT /api/matches/{id}/status.</summary>
public sealed record UpdateMatchStatusRequest(string Status);

/// <summary>Тіло POST /api/matches/generate.</summary>
public sealed record GenerateMatchesRequest(string UserId);
