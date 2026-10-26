using Matching.Application.Common;
using Matching.Application.Common.Models;

namespace Matching.Application.IndexEntries.Commands.UpdateIndexEntry;

/// <summary>
/// Оновлення за подією UserSkillsChanged. ExpectedVersion — версія документа, яку бачив клієнт
/// (заголовок If-Match). Повертає нову версію.
/// </summary>
public sealed record UpdateIndexEntryCommand(
    string Id, int ExpectedVersion, string SkillName, int Level, string Status, int? ExperienceYears,
    IReadOnlyList<LanguageItem> Languages, IReadOnlyList<AvailabilityItem> Availability) : ICommand<int>;
