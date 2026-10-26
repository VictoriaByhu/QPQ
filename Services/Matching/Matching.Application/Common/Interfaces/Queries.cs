using Matching.Application.Common.Models;

namespace Matching.Application.Common.Interfaces;

/// <summary>
/// Читання (Query-сторона CQRS). Реалізація в Infrastructure будує проєкцію в DTO прямо в MongoDB,
/// тому обробники запитів не залежать від драйвера й не завантажують повні документи.
/// </summary>
public interface IUserSkillIndexQueries
{
    Task<IndexEntryDto?> GetByIdAsync(string id, CancellationToken ct);
    Task<PagedResult<IndexEntryDto>> GetListAsync(
        string? userId, int? skillId, string? type, int page, int pageSize, CancellationToken ct);
}

public interface IMatchQueries
{
    Task<MatchDto?> GetByIdAsync(string id, CancellationToken ct);
    Task<PagedResult<MatchDto>> GetForUserAsync(
        string userId, bool includeDismissed, int page, int pageSize, CancellationToken ct);
}

public interface ISkillStatsQueries
{
    Task<SkillStatsDto?> GetByIdAsync(int skillId, CancellationToken ct);
    Task<IReadOnlyList<SkillStatsDto>> GetAllAsync(CancellationToken ct);
}
