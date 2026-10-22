using Ardalis.Specification;
using Sessions.Domain.Models;

namespace Sessions.Bll.Specifications;

public sealed class SkillsByIdsSpec : Specification<Skill>
{
    public SkillsByIdsSpec(IReadOnlyCollection<int> ids)
    {
        Query.Where(s => ids.Contains(s.Id) && !s.IsDeleted);
    }
}
