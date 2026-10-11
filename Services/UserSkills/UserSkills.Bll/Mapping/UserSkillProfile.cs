using AutoMapper;
using UserSkills.Bll.Dtos;
using UserSkills.Domain.Models;

namespace UserSkills.Bll.Mapping;

/// <summary>Усі мапінги модель -> DTO в одному місці; конфігурація перевіряється тестом.</summary>
public class UserSkillProfile : Profile
{
    public UserSkillProfile()
    {
        CreateMap<Skill, SkillDto>();
        CreateMap<Language, LanguageRefDto>();
        CreateMap<UserSkillLanguage, LanguageDto>();
        CreateMap<UserSkillDetails, UserSkillDetailsDto>();

        CreateMap<AvailabilitySlot, AvailabilitySlotDto>()
            .ForCtorParam(nameof(AvailabilitySlotDto.StartTime),
                o => o.MapFrom(s => s.StartTime.ToString(@"hh\:mm")))
            .ForCtorParam(nameof(AvailabilitySlotDto.EndTime),
                o => o.MapFrom(s => s.EndTime.ToString(@"hh\:mm")));

        CreateMap<UserSkill, UserSkillSummaryDto>();
        CreateMap<UserSkill, UserSkillDto>();
        CreateMap<UserSkillStatusHistoryEntry, StatusHistoryDto>();
    }
}
