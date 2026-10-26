using Matching.Application.Common.Models;
using Matching.Domain.ValueObjects;

namespace Matching.Application.Common.Mapping;

internal static class InputMapping
{
    public static IEnumerable<Language> ToDomain(this IEnumerable<LanguageItem> items)
        => items.Select(i => new Language(i.Code, i.Proficiency));

    public static IEnumerable<AvailabilitySlot> ToDomain(this IEnumerable<AvailabilityItem> items)
        => items.Select(i => new AvailabilitySlot(i.DayOfWeek, i.Start, i.End));
}
