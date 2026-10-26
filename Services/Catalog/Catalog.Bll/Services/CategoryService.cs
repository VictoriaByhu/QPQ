using AutoMapper;
using Catalog.Bll.Dtos;
using Catalog.Dal.Interfaces;
using Catalog.Domain.Exceptions;
using Catalog.Domain.Models;
using Catalog.Domain.Specifications;

namespace Catalog.Bll.Services;

public class CategoryService(IUnitOfWork uow, IMapper mapper) : ICategoryService
{
    public async Task<PagedResult<CategoryDto>> GetListAsync(CategoryQuery query, CancellationToken ct = default)
    {
        var page = Paging.NormalizePage(query.Page);
        var pageSize = Paging.NormalizePageSize(query.PageSize);
        var filter = new CategoryFilter(
            query.Search?.Trim(),
            string.Equals(query.SortDir, "desc", StringComparison.OrdinalIgnoreCase),
            (page - 1) * pageSize,
            pageSize);

        var total = await uow.Categories.CountAsync(new CategoriesFilterSpec(filter, paged: false), ct);
        var items = await uow.Categories.ListAsync(new CategoriesFilterSpec(filter), ct);

        return new PagedResult<CategoryDto>(mapper.Map<List<CategoryDto>>(items), total, page, pageSize);
    }

    public async Task<CategoryWithSkillsDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        // Explicit Loading у репозиторії: навички категорії довантажуються окремим запитом.
        var category = await uow.Categories.GetWithSkillsAsync(id, ct)
                       ?? throw new EntityNotFoundException($"Category {id} was not found.");
        return mapper.Map<CategoryWithSkillsDto>(category);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto input, CancellationToken ct = default)
    {
        await EnsureUniqueAsync(input.Name.Trim(), input.Slug.Trim(), exceptId: null, ct);

        var category = new Category(input.Name, input.Slug, input.SortOrder);
        await uow.Categories.AddAsync(category, ct);
        await uow.SaveChangesAsync(ct);
        return mapper.Map<CategoryDto>(category);
    }

    public async Task<CategoryDto> UpdateAsync(int id, UpdateCategoryDto input, CancellationToken ct = default)
    {
        var category = await FindActiveAsync(id, ct);
        await EnsureUniqueAsync(input.Name.Trim(), input.Slug.Trim(), exceptId: id, ct);

        var rowVersion = RowVersionHelper.Decode(input.RowVersion);
        if (rowVersion is not null)
            uow.Categories.SetOriginalRowVersion(category, rowVersion);

        category.Update(input.Name, input.Slug, input.SortOrder);
        await uow.SaveChangesAsync(ct);
        return mapper.Map<CategoryDto>(category);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var category = await FindActiveAsync(id, ct);
        if (await uow.Categories.HasActiveSkillsAsync(id, ct))
            throw new ConflictException($"Category {id} still has active skills and cannot be deleted.");

        category.SoftDelete();
        await uow.SaveChangesAsync(ct);
    }

    private async Task<Category> FindActiveAsync(int id, CancellationToken ct)
    {
        var category = await uow.Categories.GetByIdAsync(id, ct);
        if (category is null || category.IsDeleted)
            throw new EntityNotFoundException($"Category {id} was not found.");
        return category;
    }

    private async Task EnsureUniqueAsync(string name, string slug, int? exceptId, CancellationToken ct)
    {
        if (await uow.Categories.ExistsByNameAsync(name, exceptId, ct))
            throw new ConflictException($"Category with name '{name}' already exists.");
        if (await uow.Categories.ExistsBySlugAsync(slug, exceptId, ct))
            throw new ConflictException($"Category with slug '{slug}' already exists.");
    }
}
