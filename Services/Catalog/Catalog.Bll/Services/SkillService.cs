using AutoMapper;
using Catalog.Bll.Dtos;
using Catalog.Dal.Interfaces;
using Catalog.Domain.Exceptions;
using Catalog.Domain.Models;
using Catalog.Domain.Specifications;

namespace Catalog.Bll.Services;

/// <summary>
/// Бізнес-логіка навичок. Працює лише через репозиторії й Unit of Work; правила самої сутності
/// перевіряє сутність, а сервіс — ті, для яких потрібні інші дані з бази (унікальність, існування).
/// </summary>
public class SkillService(IUnitOfWork uow, IMapper mapper) : ISkillService
{
    public async Task<PagedResult<SkillListItemDto>> GetListAsync(SkillQuery query, CancellationToken ct = default)
    {
        var page = Paging.NormalizePage(query.Page);
        var pageSize = Paging.NormalizePageSize(query.PageSize);

        var filter = new SkillFilter(
            Search: query.Search?.Trim(),
            CategoryId: query.CategoryId,
            Tag: query.Tag?.Trim(),
            SortBy: ParseSort(query.SortBy),
            Descending: string.Equals(query.SortDir, "desc", StringComparison.OrdinalIgnoreCase),
            Skip: (page - 1) * pageSize,
            Take: pageSize);

        // totalCount рахує база за тими самими критеріями, а сторінку (OFFSET/FETCH) віддає другий запит.
        var total = await uow.Skills.CountAsync(new SkillsFilterSpec(filter, paged: false), ct);
        var items = await uow.Skills.ListAsync(new SkillsFilterSpec(filter), ct);

        return new PagedResult<SkillListItemDto>(mapper.Map<List<SkillListItemDto>>(items), total, page, pageSize);
    }

    public async Task<SkillDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var skill = await uow.Skills.GetWithDetailsAsync(id, ct)
                    ?? throw new EntityNotFoundException($"Skill {id} was not found.");
        return mapper.Map<SkillDto>(skill);
    }

    public async Task<SkillDto> CreateAsync(CreateSkillDto input, CancellationToken ct = default)
    {
        await EnsureCategoryExistsAsync(input.CategoryId, ct);
        await EnsureUniqueAsync(input.Name.Trim(), input.Slug.Trim(), exceptId: null, ct);

        var skill = new Skill(input.CategoryId, input.Name, input.Slug);
        skill.SetDetail(input.Description, input.IconUrl);
        skill.AddDefaultLevels();

        foreach (var tagId in input.TagIds ?? [])
        {
            if (await uow.Tags.GetByIdAsync(tagId, ct) is null)
                throw new ValidationException(nameof(input.TagIds), $"Tag {tagId} does not exist.");
            skill.AddTag(tagId);
        }

        await uow.Skills.AddAsync(skill, ct);
        await uow.SaveChangesAsync(ct);

        return await GetByIdAsync(skill.Id, ct);
    }

    public async Task<SkillDto> UpdateAsync(int id, UpdateSkillDto input, CancellationToken ct = default)
    {
        var skill = await uow.Skills.GetWithDetailsAsync(id, ct)
                    ?? throw new EntityNotFoundException($"Skill {id} was not found.");

        if (skill.CategoryId != input.CategoryId)
            await EnsureCategoryExistsAsync(input.CategoryId, ct);
        await EnsureUniqueAsync(input.Name.Trim(), input.Slug.Trim(), exceptId: id, ct);

        var rowVersion = RowVersionHelper.Decode(input.RowVersion);
        if (rowVersion is not null)
            uow.Skills.SetOriginalRowVersion(skill, rowVersion);

        skill.Update(input.CategoryId, input.Name, input.Slug);
        skill.SetDetail(input.Description, input.IconUrl);

        await uow.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var skill = await uow.Skills.GetByIdAsync(id, ct);
        if (skill is null || skill.IsDeleted)
            throw new EntityNotFoundException($"Skill {id} was not found.");

        skill.SoftDelete();
        await uow.SaveChangesAsync(ct);
    }

    public async Task AddTagAsync(int skillId, int tagId, CancellationToken ct = default)
    {
        var skill = await uow.Skills.GetWithDetailsAsync(skillId, ct)
                    ?? throw new EntityNotFoundException($"Skill {skillId} was not found.");
        if (await uow.Tags.GetByIdAsync(tagId, ct) is null)
            throw new EntityNotFoundException($"Tag {tagId} was not found.");

        skill.AddTag(tagId);                       // повторне додавання -> DomainException (409)
        await uow.SaveChangesAsync(ct);
    }

    public async Task RemoveTagAsync(int skillId, int tagId, CancellationToken ct = default)
    {
        var skill = await uow.Skills.GetWithDetailsAsync(skillId, ct)
                    ?? throw new EntityNotFoundException($"Skill {skillId} was not found.");

        skill.RemoveTag(tagId);
        await uow.SaveChangesAsync(ct);
    }

    public async Task<SkillLevelDto> UpdateLevelAsync(
        int skillId, byte level, UpdateSkillLevelDto input, CancellationToken ct = default)
    {
        var skill = await uow.Skills.GetWithDetailsAsync(skillId, ct)
                    ?? throw new EntityNotFoundException($"Skill {skillId} was not found.");

        var updated = skill.RenameLevel(level, input.Title, input.Description);
        await uow.SaveChangesAsync(ct);
        return mapper.Map<SkillLevelDto>(updated);
    }

    public async Task<IReadOnlyList<SkillBriefDto>> GetRecentlyTaggedAsync(
        string tagName, DateTime since, CancellationToken ct = default)
    {
        var skills = await uow.SkillTags.GetRecentlyTaggedAsync(tagName.Trim(), since, ct);
        return mapper.Map<List<SkillBriefDto>>(skills);
    }

    private static SkillSortField ParseSort(string? sortBy) => sortBy?.ToLowerInvariant() switch
    {
        "category" => SkillSortField.Category,
        "createdat" => SkillSortField.CreatedAt,
        _ => SkillSortField.Name
    };

    private async Task EnsureCategoryExistsAsync(int categoryId, CancellationToken ct)
    {
        var category = await uow.Categories.GetByIdAsync(categoryId, ct);
        if (category is null || category.IsDeleted)
            throw new ValidationException("categoryId", $"Category {categoryId} does not exist.");
    }

    private async Task EnsureUniqueAsync(string name, string slug, int? exceptId, CancellationToken ct)
    {
        if (await uow.Skills.ExistsByNameAsync(name, exceptId, ct))
            throw new ConflictException($"Skill with name '{name}' already exists.");
        if (await uow.Skills.ExistsBySlugAsync(slug, exceptId, ct))
            throw new ConflictException($"Skill with slug '{slug}' already exists.");
    }
}
