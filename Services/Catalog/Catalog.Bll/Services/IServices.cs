using Catalog.Bll.Dtos;

namespace Catalog.Bll.Services;

public interface ISkillService
{
    Task<PagedResult<SkillListItemDto>> GetListAsync(SkillQuery query, CancellationToken ct = default);
    Task<SkillDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<SkillDto> CreateAsync(CreateSkillDto input, CancellationToken ct = default);
    Task<SkillDto> UpdateAsync(int id, UpdateSkillDto input, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task AddTagAsync(int skillId, int tagId, CancellationToken ct = default);
    Task RemoveTagAsync(int skillId, int tagId, CancellationToken ct = default);
    Task<SkillLevelDto> UpdateLevelAsync(int skillId, byte level, UpdateSkillLevelDto input, CancellationToken ct = default);
    Task<IReadOnlyList<SkillBriefDto>> GetRecentlyTaggedAsync(string tagName, DateTime since, CancellationToken ct = default);
}

public interface ICategoryService
{
    Task<PagedResult<CategoryDto>> GetListAsync(CategoryQuery query, CancellationToken ct = default);
    Task<CategoryWithSkillsDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<CategoryDto> CreateAsync(CreateCategoryDto input, CancellationToken ct = default);
    Task<CategoryDto> UpdateAsync(int id, UpdateCategoryDto input, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public interface ITagService
{
    Task<IReadOnlyList<TagDto>> GetAllAsync(CancellationToken ct = default);
    Task<TagDto> CreateAsync(CreateTagDto input, CancellationToken ct = default);
}
