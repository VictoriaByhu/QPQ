using Swaps.Bll.Dtos;
using Swaps.Bll.Services;

namespace Swaps.Bll.Caching;

public sealed class CachedSkillService(ISkillService inner, TwoLevelCache cache) : ISkillService
{
    public async Task<IReadOnlyList<SkillDto>> GetAllAsync(CancellationToken ct = default)
    {
        var skills = await cache.GetOrCreateAsync<List<SkillDto>>(
            CacheKeys.AllSkills,
            async token => [.. await inner.GetAllAsync(token)],
            CacheTtl.Skills,
            ct);

        return skills ?? [];
    }

    public async Task<SkillDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        // A missing skill makes the inner service throw, so "not found" is never cached.
        var skill = await cache.GetOrCreateAsync<SkillDto>(
            CacheKeys.Skill(id),
            async token => await inner.GetByIdAsync(id, token),
            CacheTtl.Skills,
            ct);

        return skill!;
    }
}
