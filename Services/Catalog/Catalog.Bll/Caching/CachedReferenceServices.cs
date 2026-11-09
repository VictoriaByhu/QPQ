using Catalog.Bll.Dtos;
using Catalog.Bll.Services;

namespace Catalog.Bll.Caching;

/// <summary>Теги — довідник, що майже не змінюється: весь список під одним ключем з довгим TTL.</summary>
public sealed class CachedTagService(ITagService inner, ITwoLevelCache cache) : ITagService
{
    public const string AllKey = "catalog:tags:all";

    public async Task<IReadOnlyList<TagDto>> GetAllAsync(CancellationToken ct = default)
    {
        // IReadOnlyList не десеріалізується в інтерфейс як корінь — кешуємо як List.
        var list = await cache.GetOrCreateAsync(AllKey, CachePolicy.Reference,
            async c => (await inner.GetAllAsync(c)).ToList(), ct);
        return list!;
    }

    public async Task<TagDto> CreateAsync(CreateTagDto input, CancellationToken ct = default)
    {
        var created = await inner.CreateAsync(input, ct);
        await cache.RemoveAsync(AllKey, "TagCreated", ct);
        return created;
    }
}

/// <summary>
/// Категорії: читання кешуються, а зміна категорії інвалідує ще й її навички — у картці й списку навичок
/// є назва категорії, тому без цього вони показували б стару.
/// </summary>
public sealed class CachedCategoryService(ICategoryService inner, ITwoLevelCache cache) : ICategoryService
{
    public const string ListGroup = "categories-list";
    public static string Key(int id) => $"catalog:category:{id}";

    public async Task<PagedResult<CategoryDto>> GetListAsync(CategoryQuery query, CancellationToken ct = default)
    {
        var version = await cache.GetVersionAsync(ListGroup, ct);
        var key = $"catalog:categories:list:{version}:{Paging.NormalizePage(query.Page)}:{Paging.NormalizePageSize(query.PageSize)}:" +
                  $"{query.Search?.Trim().ToLowerInvariant()}:{query.SortDir.ToLowerInvariant()}";
        var result = await cache.GetOrCreateAsync(key, CachePolicy.Reference, c => inner.GetListAsync(query, c)!, ct);
        return result!;
    }

    public async Task<CategoryWithSkillsDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var dto = await cache.GetOrCreateAsync(Key(id), CachePolicy.Entity, c => inner.GetByIdAsync(id, c)!, ct);
        return dto!;
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto input, CancellationToken ct = default)
    {
        var created = await inner.CreateAsync(input, ct);
        await cache.BumpVersionAsync(ListGroup, "CategoryCreated", ct);
        return created;
    }

    public async Task<CategoryDto> UpdateAsync(int id, UpdateCategoryDto input, CancellationToken ct = default)
    {
        var skillIds = await SkillIdsAsync(id, ct);                              // до запису: після нього категорії вже може не бути
        var updated = await inner.UpdateAsync(id, input, ct);
        await InvalidateAsync(id, skillIds, "CategoryUpdated", ct);
        return updated;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var skillIds = await SkillIdsAsync(id, ct);
        await inner.DeleteAsync(id, ct);
        await InvalidateAsync(id, skillIds, "CategoryDeleted", ct);
    }

    private async Task<int[]> SkillIdsAsync(int id, CancellationToken ct)
    {
        try { return (await GetByIdAsync(id, ct)).Skills.Select(s => s.Id).ToArray(); }
        catch (Domain.Exceptions.EntityNotFoundException) { return []; }        // inner.Update/Delete сам поверне 404
    }

    private async Task InvalidateAsync(int id, int[] skillIds, string reason, CancellationToken ct)
    {
        await cache.RemoveAsync(Key(id), reason, ct);
        foreach (var skillId in skillIds)
            await cache.RemoveAsync(CachedSkillService.Key(skillId), reason, ct);
        await cache.BumpVersionAsync(ListGroup, reason, ct);
        await cache.BumpVersionAsync(CachedSkillService.ListGroup, reason, ct);
    }
}
