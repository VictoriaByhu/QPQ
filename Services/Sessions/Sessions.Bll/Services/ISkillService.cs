using Sessions.Bll.Dtos;

namespace Sessions.Bll.Services;

public interface ISkillService
{
    Task<IReadOnlyList<SkillDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SkillSessionDto>> GetSessionsAsync(
        Guid userId, int skillId, DateTime? from, CancellationToken ct = default);
}
