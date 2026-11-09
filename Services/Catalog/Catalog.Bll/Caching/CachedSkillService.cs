using Platform.Caching;
using System.Security.Cryptography;
using System.Text;
using Catalog.Bll.Dtos;
using Catalog.Bll.Services;

namespace Catalog.Bll.Caching;

/// <summary>
/// Декоратор над <see cref="SkillService"/>: читання (картка й список) проходять через двохрівневий кеш,
/// кожна зміна синхронно інвалідує пов'язані записи. REST-контролер і gRPC-сервіс залежать від ISkillService,
/// тому обидва протоколи користуються одним кешем без дублювання коду.
/// Ключі: catalog:skill:{id} (картка), catalog:skills:list:{версія}:{хеш запиту} (сторінки списку).
/// </summary>
public sealed class CachedSkillService(ISkillService inner, ITwoLevelCache cache) : ISkillService
{
    public const string ListGroup = "skills-list";
    public static string Key(int id) => $"catalog:skill:{id}";

    public async Task<SkillDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        // Відсутня навичка → inner кидає EntityNotFoundException: виняток не кешується, 404 віддається щоразу з бази.
        var dto = await cache.GetOrCreateAsync(Key(id), CachePolicy.Entity,
            c => inner.GetByIdAsync(id, c)!, ct);
        return dto!;
    }

    public async Task<PagedResult<SkillListItemDto>> GetListAsync(SkillQuery query, CancellationToken ct = default)
    {
        var version = await cache.GetVersionAsync(ListGroup, ct);
        var key = $"catalog:skills:list:{version}:{Hash(query)}";
        var result = await cache.GetOrCreateAsync(key, CachePolicy.List, c => inner.GetListAsync(query, c)!, ct);
        return result!;
    }

    public Task<IReadOnlyList<SkillBriefDto>> GetRecentlyTaggedAsync(string tagName, DateTime since, CancellationToken ct = default)
        => inner.GetRecentlyTaggedAsync(tagName, since, ct);                      // рідкісний запит з параметром дати — не кешується

    public async Task<SkillDto> CreateAsync(CreateSkillDto input, CancellationToken ct = default)
    {
        var created = await inner.CreateAsync(input, ct);
        await InvalidateListsAsync("SkillCreated", ct);
        return created;
    }

    public async Task<SkillDto> UpdateAsync(int id, UpdateSkillDto input, CancellationToken ct = default)
    {
        var updated = await inner.UpdateAsync(id, input, ct);
        await InvalidateAsync(id, "SkillUpdated", ct);
        return updated;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await inner.DeleteAsync(id, ct);
        await InvalidateAsync(id, "SkillDeleted", ct);
    }

    public async Task AddTagAsync(int skillId, int tagId, CancellationToken ct = default)
    {
        await inner.AddTagAsync(skillId, tagId, ct);
        await InvalidateAsync(skillId, "SkillTagAdded", ct);
    }

    public async Task RemoveTagAsync(int skillId, int tagId, CancellationToken ct = default)
    {
        await inner.RemoveTagAsync(skillId, tagId, ct);
        await InvalidateAsync(skillId, "SkillTagRemoved", ct);
    }

    public async Task<SkillLevelDto> UpdateLevelAsync(
        int skillId, byte level, UpdateSkillLevelDto input, CancellationToken ct = default)
    {
        var updated = await inner.UpdateLevelAsync(skillId, level, input, ct);
        await InvalidateAsync(skillId, "SkillLevelUpdated", ct);
        return updated;
    }

    /// <summary>Картка змінилась → видаляємо її ключ і переводимо списки на нову версію (у них є назва, категорія, теги).</summary>
    private async Task InvalidateAsync(int id, string reason, CancellationToken ct)
    {
        await cache.RemoveAsync(Key(id), reason, ct);
        await InvalidateListsAsync(reason, ct);
    }

    private Task InvalidateListsAsync(string reason, CancellationToken ct) => cache.BumpVersionAsync(ListGroup, reason, ct);

    /// <summary>Однаковий запит (після нормалізації) → однаковий ключ.</summary>
    private static string Hash(SkillQuery q)
    {
        var page = Paging.NormalizePage(q.Page);
        var size = Paging.NormalizePageSize(q.PageSize);
        var raw = string.Join('|', page, size, q.Search?.Trim().ToLowerInvariant(), q.CategoryId, q.Tag?.Trim().ToLowerInvariant(),
            q.SortBy.ToLowerInvariant(), q.SortDir.ToLowerInvariant());
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)))[..16];
    }
}
