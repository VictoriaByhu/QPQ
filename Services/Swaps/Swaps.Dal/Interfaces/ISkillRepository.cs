using Swaps.Domain.Models;

namespace Swaps.Dal.Interfaces;

public interface ISkillRepository : IGenericRepository<Skill>
{
    Task<IReadOnlyList<Skill>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);
}
