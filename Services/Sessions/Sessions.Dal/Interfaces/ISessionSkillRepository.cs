using Sessions.Domain.Models;

namespace Sessions.Dal.Interfaces;

public interface ISessionSkillRepository : IGenericRepository<SessionSkill>
{
    Task<IReadOnlyList<SessionSkill>> GetBySkillForUserAsync(
        int skillId, Guid userId, DateTime? from, CancellationToken ct = default);
}
