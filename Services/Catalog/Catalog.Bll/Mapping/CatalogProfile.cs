using AutoMapper;
using Catalog.Bll.Dtos;
using Catalog.Domain.Models;

namespace Catalog.Bll.Mapping;

/// <summary>Усі мапінги Entity -> DTO в одному місці; конфігурація перевіряється тестом.</summary>
public class CatalogProfile : Profile
{
    public CatalogProfile()
    {
        CreateMap<SkillLevel, SkillLevelDto>();
        CreateMap<Skill, SkillBriefDto>();
        CreateMap<Tag, TagDto>();

        CreateMap<Skill, SkillListItemDto>()
            .ForCtorParam(nameof(SkillListItemDto.CategoryName), o => o.MapFrom(s => s.Category.Name))
            .ForCtorParam(nameof(SkillListItemDto.Tags),
                o => o.MapFrom(s => s.SkillTags.Select(st => st.Tag.Name).OrderBy(n => n)));

        CreateMap<Skill, SkillDto>()
            .ForCtorParam(nameof(SkillDto.CategoryName), o => o.MapFrom(s => s.Category.Name))
            .ForCtorParam(nameof(SkillDto.Description), o => o.MapFrom(s => s.Detail == null ? null : s.Detail.Description))
            .ForCtorParam(nameof(SkillDto.IconUrl), o => o.MapFrom(s => s.Detail == null ? null : s.Detail.IconUrl))
            .ForCtorParam(nameof(SkillDto.Levels), o => o.MapFrom(s => s.Levels.OrderBy(l => l.Level)))
            .ForCtorParam(nameof(SkillDto.Tags),
                o => o.MapFrom(s => s.SkillTags.Select(st => st.Tag.Name).OrderBy(n => n)))
            .ForCtorParam(nameof(SkillDto.RowVersion), o => o.MapFrom(s => Convert.ToBase64String(s.RowVersion)));

        CreateMap<Category, CategoryDto>()
            .ForCtorParam(nameof(CategoryDto.RowVersion), o => o.MapFrom(c => Convert.ToBase64String(c.RowVersion)));

        CreateMap<Category, CategoryWithSkillsDto>()
            .ForCtorParam(nameof(CategoryWithSkillsDto.RowVersion), o => o.MapFrom(c => Convert.ToBase64String(c.RowVersion)));
    }
}
