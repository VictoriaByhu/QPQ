using UserSkills.Bll.Dtos;

namespace UserSkills.Bll.Services;

public interface IUserSkillService
{
    Task<IReadOnlyList<UserSkillSummaryDto>> ListAsync(
        Guid userId, string? type, string? status, int pageNumber, int pageSize,
        CancellationToken ct = default);

    Task<UserSkillDto> GetByIdAsync(int id, CancellationToken ct = default);

    Task<UserSkillDto> CreateAsync(Guid userId, CreateUserSkillDto input, CancellationToken ct = default);

    Task<UserSkillDto> UpdateAsync(
        Guid userId, int id, UpdateUserSkillDto input, CancellationToken ct = default);

    Task<RowVersionDto> ChangeStatusAsync(
        Guid userId, int id, ChangeStatusDto input, CancellationToken ct = default);

    Task DeleteAsync(Guid userId, int id, byte[] rowVersion, CancellationToken ct = default);

    Task<IReadOnlyList<StatusHistoryDto>> GetHistoryAsync(
        Guid userId, int id, CancellationToken ct = default);
}

/// <summary>Довідники: локальна копія навичок із Catalog і мови.</summary>
public interface IReferenceDataService
{
    Task<IReadOnlyList<SkillDto>> GetSkillsAsync(CancellationToken ct = default);
    Task<SkillDto> GetSkillAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<LanguageRefDto>> GetLanguagesAsync(CancellationToken ct = default);
    Task<LanguageRefDto> GetLanguageAsync(string code, CancellationToken ct = default);
}
