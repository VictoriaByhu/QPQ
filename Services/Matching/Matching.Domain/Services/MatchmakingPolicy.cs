using Matching.Domain.Entities;
using Matching.Domain.Enums;

namespace Matching.Domain.Services;

/// <summary>
/// Доменні правила підбору пар (чиста логіка без доступу до БД).
/// Пара «хочу — пропоную» можлива, якщо навичка однакова, обидва записи активні,
/// рівень пропозиції не нижчий за бажаний і є хоча б одна спільна мова.
/// </summary>
public static class MatchmakingPolicy
{
    public static bool IsCompatible(UserSkillIndexEntry want, UserSkillIndexEntry offer)
        => want.Type == UserSkillType.Want
           && offer.Type == UserSkillType.Offer
           && want.Status == UserSkillStatus.Active
           && offer.Status == UserSkillStatus.Active
           && want.SkillId == offer.SkillId
           && want.UserId != offer.UserId
           && offer.Level >= want.Level
           && CommonLanguages(want, offer).Count > 0;

    public static IReadOnlyList<string> CommonLanguages(UserSkillIndexEntry a, UserSkillIndexEntry b)
        => a.Languages.Select(l => l.Code)
            .Intersect(b.Languages.Select(l => l.Code))
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();

    public static bool AvailabilityOverlaps(UserSkillIndexEntry a, UserSkillIndexEntry b)
        => a.Availability.Any(x => b.Availability.Any(x.Overlaps));

    /// <summary>База 50; до +10 за спільні мови; +15 за збіг вікон; +25 за взаємність; максимум 100.</summary>
    public static int Score(UserSkillIndexEntry want, UserSkillIndexEntry offer, bool mutual)
    {
        var score = 50 + Math.Min(CommonLanguages(want, offer).Count, 2) * 5;
        if (AvailabilityOverlaps(want, offer)) score += 15;
        if (mutual) score += 25;
        return Math.Min(score, 100);
    }
}
