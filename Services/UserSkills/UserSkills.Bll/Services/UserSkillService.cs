using System.Data;
using System.Globalization;
using AutoMapper;
using UserSkills.Bll.Dtos;
using UserSkills.Dal.Interfaces;
using UserSkills.Domain.Constants;
using UserSkills.Domain.Exceptions;
using UserSkills.Domain.Models;

namespace UserSkills.Bll.Services;

/// <summary>
/// Єдиний власник бізнес-логіки UserSkills. Контролери лише передають дані й повертають результат.
/// Ізоляція транзакцій: ReadCommitted (рівень за замовчуванням SQL Server). Процедури з Лаб. 1
/// додатково беруть UPDLOCK, HOLDLOCK на рядок, що змінюється, тому гонки за один запис виключені,
/// а читання інших рядків не блокується.
/// </summary>
public sealed class UserSkillService(IUnitOfWork uow, IMapper mapper) : IUserSkillService
{
    private const IsolationLevel Isolation = IsolationLevel.ReadCommitted;

    public async Task<IReadOnlyList<UserSkillSummaryDto>> ListAsync(
        Guid userId, string? type, string? status, int pageNumber, int pageSize,
        CancellationToken ct = default)
    {
        if (type is not null && !UserSkillTypes.IsValid(type))
            throw new ValidationException("Type must be Offer or Want.");
        if (status is not null && !UserSkillStatuses.IsValid(status))
            throw new ValidationException("Status must be Active, Paused or Archived.");
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
            throw new ValidationException("pageNumber must be >= 1 and pageSize must be in 1..100.");

        var rows = await uow.UserSkills.ListByUserAsync(userId, type, status, pageNumber, pageSize, ct);
        return mapper.Map<IReadOnlyList<UserSkillSummaryDto>>(rows);
    }

    public async Task<IReadOnlyList<UserSkillSummaryDto>> ListBySkillAsync(
        int skillId, string? type, int limit, CancellationToken ct = default)
    {
        if (skillId < 1)
            throw new ValidationException("skillId must be positive.");
        if (type is not null && !UserSkillTypes.IsValid(type))
            throw new ValidationException("Type must be Offer or Want.");
        if (limit < 1 || limit > 100)
            throw new ValidationException("limit must be in 1..100.");

        var rows = await uow.UserSkills.ListBySkillAsync(skillId, type, limit, ct);
        return mapper.Map<IReadOnlyList<UserSkillSummaryDto>>(rows);
    }

    public async Task<UserSkillDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var userSkill = await uow.UserSkills.GetFullAsync(id, ct)
                        ?? throw new NotFoundException($"User skill {id} not found");
        return mapper.Map<UserSkillDto>(userSkill);
    }

    /// <summary>
    /// Транзакційна бізнес-операція: перевірка навички (SkillRepository, Dapper), мов
    /// (LanguageRepository, ADO.NET) і створення запису разом з деталями, мовами, доступністю
    /// та журналом (UserSkillRepository, usp_AddUserSkill) в одній транзакції.
    /// </summary>
    public async Task<UserSkillDto> CreateAsync(
        Guid userId, CreateUserSkillDto input, CancellationToken ct = default)
    {
        var languages = ToLanguages(input.Languages);
        var slots = ToSlots(input.Slots);

        int newId;
        await uow.BeginTransactionAsync(Isolation, ct);
        try
        {
            _ = await uow.Skills.GetByIdAsync(input.SkillId, ct)
                ?? throw new NotFoundException($"Skill {input.SkillId} not found");

            await EnsureLanguagesExistAsync(languages, ct);

            newId = await uow.UserSkills.AddFullAsync(new UserSkill
            {
                UserId = userId,
                SkillId = input.SkillId,
                Type = input.Type,
                Level = input.Level,
                Details = new UserSkillDetails
                {
                    Description = input.Description,
                    ExperienceYears = input.ExperienceYears,
                    PortfolioUrl = input.PortfolioUrl
                },
                Languages = languages,
                Slots = slots
            }, ct);

            await uow.CommitAsync(ct);
        }
        catch
        {
            await uow.RollbackAsync(ct); // жодних часткових змін
            throw;
        }

        return await GetByIdAsync(newId, ct);
    }

    public async Task<UserSkillDto> UpdateAsync(
        Guid userId, int id, UpdateUserSkillDto input, CancellationToken ct = default)
    {
        var rowVersion = RequireRowVersion(input.RowVersion);
        var languages = ToLanguages(input.Languages);
        var slots = ToSlots(input.Slots);

        await uow.BeginTransactionAsync(Isolation, ct);
        try
        {
            await EnsureLanguagesExistAsync(languages, ct);

            await uow.UserSkills.UpdateFullAsync(id, userId, rowVersion, new UserSkill
            {
                Level = input.Level,
                Details = new UserSkillDetails
                {
                    Description = input.Description,
                    ExperienceYears = input.ExperienceYears,
                    PortfolioUrl = input.PortfolioUrl
                },
                Languages = languages,
                Slots = slots
            }, ct);

            await uow.CommitAsync(ct);
        }
        catch
        {
            await uow.RollbackAsync(ct);
            throw;
        }

        return await GetByIdAsync(id, ct);
    }

    public async Task<RowVersionDto> ChangeStatusAsync(
        Guid userId, int id, ChangeStatusDto input, CancellationToken ct = default)
    {
        if (!UserSkillStatuses.IsValid(input.NewStatus))
            throw new ValidationException("NewStatus must be Active, Paused or Archived.");
        var rowVersion = RequireRowVersion(input.RowVersion);

        await uow.BeginTransactionAsync(Isolation, ct);
        try
        {
            var newRowVersion = await uow.UserSkills.ChangeStatusAsync(
                id, input.NewStatus, userId, rowVersion, input.Comment, ct);
            await uow.CommitAsync(ct);
            return new RowVersionDto(newRowVersion);
        }
        catch
        {
            await uow.RollbackAsync(ct);
            throw;
        }
    }

    public async Task DeleteAsync(Guid userId, int id, byte[] rowVersion, CancellationToken ct = default)
    {
        var token = RequireRowVersion(rowVersion);

        await uow.BeginTransactionAsync(Isolation, ct);
        try
        {
            await uow.UserSkills.SoftDeleteAsync(id, userId, token, ct);
            await uow.CommitAsync(ct);
        }
        catch
        {
            await uow.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<IReadOnlyList<StatusHistoryDto>> GetHistoryAsync(
        Guid userId, int id, CancellationToken ct = default)
    {
        var userSkill = await uow.UserSkills.GetByIdAsync(id, ct)
                        ?? throw new NotFoundException($"User skill {id} not found");
        if (userSkill.UserId != userId)
            throw new ForbiddenException("Only the owner can perform this action.");

        var history = await uow.StatusHistory.GetByUserSkillAsync(id, ct);
        return mapper.Map<IReadOnlyList<StatusHistoryDto>>(history);
    }

    // ---- бізнес-валідація (потрібен стан із БД або складніші правила, ніж DataAnnotations) ----

    private async Task EnsureLanguagesExistAsync(
        IReadOnlyList<UserSkillLanguage> languages, CancellationToken ct)
    {
        if (languages.Count == 0) return;

        var unknown = await uow.Languages.GetUnknownCodesAsync(
            languages.Select(l => l.LanguageCode).ToList(), ct);
        if (unknown.Count > 0)
            throw new ValidationException($"Unknown language code(s): {string.Join(", ", unknown)}.");
    }

    private static byte[] RequireRowVersion(byte[]? rowVersion)
    {
        if (rowVersion is not { Length: 8 })
            throw new ValidationException("RowVersion must be an 8-byte value (base64) returned by the API.");
        return rowVersion;
    }

    private static List<UserSkillLanguage> ToLanguages(IEnumerable<LanguageInputDto> input)
    {
        var result = input
            .Select(l => new UserSkillLanguage
            {
                LanguageCode = l.LanguageCode.Trim(),
                Proficiency = l.Proficiency
            })
            .ToList();

        var duplicate = result
            .GroupBy(l => l.LanguageCode, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            throw new ValidationException($"Language '{duplicate.Key}' is listed more than once.");

        return result;
    }

    private static List<AvailabilitySlot> ToSlots(IEnumerable<AvailabilitySlotInputDto> input)
    {
        var result = new List<AvailabilitySlot>();
        foreach (var slot in input)
        {
            var start = ParseTime(slot.StartTime, nameof(slot.StartTime));
            var end = ParseTime(slot.EndTime, nameof(slot.EndTime));
            if (slot.DayOfWeek is < 1 or > 7)
                throw new ValidationException("DayOfWeek must be in 1..7 (1 = Monday).");
            if (end <= start)
                throw new ValidationException("EndTime must be later than StartTime.");

            result.Add(new AvailabilitySlot { DayOfWeek = slot.DayOfWeek, StartTime = start, EndTime = end });
        }

        var duplicate = result
            .GroupBy(s => (s.DayOfWeek, s.StartTime))
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            throw new ValidationException("Two availability slots start at the same time on the same day.");

        return result;
    }

    private static TimeSpan ParseTime(string value, string field)
    {
        if (!TimeSpan.TryParseExact(value, @"hh\:mm", CultureInfo.InvariantCulture, out var time))
            throw new ValidationException($"{field} must be in HH:mm format.");
        return time;
    }
}
