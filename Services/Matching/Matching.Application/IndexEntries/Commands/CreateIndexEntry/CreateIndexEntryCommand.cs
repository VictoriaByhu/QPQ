using Matching.Application.Common;
using Matching.Application.Common.Models;

namespace Matching.Application.IndexEntries.Commands.CreateIndexEntry;

/// <summary>Створює запис індексу (копію UserSkills). Повертає Id нового документа як рядок.</summary>
public sealed record CreateIndexEntryCommand(
    int UserSkillId, string UserId, int SkillId, string SkillName,
    string Type, int Level, string Status, int? ExperienceYears,
    IReadOnlyList<LanguageItem> Languages, IReadOnlyList<AvailabilityItem> Availability) : ICommand<string>;
