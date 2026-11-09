using Platform.Caching;
using UserSkills.Bll.Dtos;
using UserSkills.Bll.Services;

namespace UserSkills.Bll.Caching;

/// <summary>
/// Декоратор довідників (навички й мови): майже не змінюються, тому живуть у кеші довго (політика Reference).
/// Відсутній запис — виняток NotFound з обробника, він не кешується й іде нагору як є.
/// </summary>
public sealed class CachedReferenceDataService(IReferenceDataService inner, ITwoLevelCache cache) : IReferenceDataService
{
    public Task<IReadOnlyList<SkillDto>> GetSkillsAsync(CancellationToken ct = default) =>
        GetAsync("userskills:ref:skills", async c => await inner.GetSkillsAsync(c), ct);

    public Task<SkillDto> GetSkillAsync(int id, CancellationToken ct = default) =>
        GetAsync($"userskills:ref:skill:{id}", c => inner.GetSkillAsync(id, c), ct);

    public Task<IReadOnlyList<LanguageRefDto>> GetLanguagesAsync(CancellationToken ct = default) =>
        GetAsync("userskills:ref:languages", async c => await inner.GetLanguagesAsync(c), ct);

    public Task<LanguageRefDto> GetLanguageAsync(string code, CancellationToken ct = default) =>
        GetAsync($"userskills:ref:language:{code.ToLowerInvariant()}", c => inner.GetLanguageAsync(code, c), ct);

    private async Task<T> GetAsync<T>(string key, Func<CancellationToken, Task<T>> load, CancellationToken ct) where T : class =>
        (await cache.GetOrCreateAsync(key, CachePolicy.Reference, async c => await load(c), ct))!;
}

/// <summary>
/// Декоратор записів користувачів. Кешується лише вибірка за навичкою (її читає gRPC-метод ListBySkill і підбір):
/// вона спільна для всіх користувачів і часто повторюється. Списки й картки одного користувача не кешуються —
/// вони несуть RowVersion для оптимістичної конкурентності, застарілий RowVersion дав би хибні конфлікти.
/// Будь-який запис піднімає версію групи, тож усі старі ключі вибірки перестають читатися.
/// </summary>
public sealed class CachedUserSkillService(IUserSkillService inner, ITwoLevelCache cache) : IUserSkillService
{
    private const string BySkillGroup = "by-skill";

    public async Task<IReadOnlyList<UserSkillSummaryDto>> ListBySkillAsync(
        int skillId, string? type, int limit, CancellationToken ct = default)
    {
        var version = await cache.GetVersionAsync(BySkillGroup, ct);
        var key = $"userskills:by-skill:{version}:{skillId}:{type ?? "-"}:{limit}";
        return (await cache.GetOrCreateAsync(key, CachePolicy.List,
            async c => await inner.ListBySkillAsync(skillId, type, limit, c), ct))!;
    }

    public async Task<UserSkillDto> CreateAsync(Guid userId, CreateUserSkillDto input, CancellationToken ct = default)
    {
        var result = await inner.CreateAsync(userId, input, ct);
        await cache.BumpVersionAsync(BySkillGroup, "userskill-created", ct);
        return result;
    }

    public async Task<UserSkillDto> UpdateAsync(Guid userId, int id, UpdateUserSkillDto input, CancellationToken ct = default)
    {
        var result = await inner.UpdateAsync(userId, id, input, ct);
        await cache.BumpVersionAsync(BySkillGroup, "userskill-updated", ct);
        return result;
    }

    public async Task<RowVersionDto> ChangeStatusAsync(Guid userId, int id, ChangeStatusDto input, CancellationToken ct = default)
    {
        var result = await inner.ChangeStatusAsync(userId, id, input, ct);
        await cache.BumpVersionAsync(BySkillGroup, "userskill-status-changed", ct);
        return result;
    }

    public async Task DeleteAsync(Guid userId, int id, byte[] rowVersion, CancellationToken ct = default)
    {
        await inner.DeleteAsync(userId, id, rowVersion, ct);
        await cache.BumpVersionAsync(BySkillGroup, "userskill-deleted", ct);
    }

    public Task<IReadOnlyList<UserSkillSummaryDto>> ListAsync(
        Guid userId, string? type, string? status, int pageNumber, int pageSize, CancellationToken ct = default) =>
        inner.ListAsync(userId, type, status, pageNumber, pageSize, ct);

    public Task<UserSkillDto> GetByIdAsync(int id, CancellationToken ct = default) => inner.GetByIdAsync(id, ct);

    public Task<IReadOnlyList<StatusHistoryDto>> GetHistoryAsync(Guid userId, int id, CancellationToken ct = default) =>
        inner.GetHistoryAsync(userId, id, ct);
}
