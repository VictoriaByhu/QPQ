using AutoMapper;
using Swaps.Bll.Dtos;
using Swaps.Domain.Models;

namespace Swaps.Bll.Mapping;

public class SwapProfile : Profile
{
    public SwapProfile()
    {
        CreateMap<Skill, SkillDto>();
        CreateMap<SwapSkill, SwapSkillDto>();
        CreateMap<SwapDetails, SwapDetailsDto>();
        CreateMap<SwapStatusHistoryEntry, SwapStatusHistoryDto>();

        CreateMap<Swap, SwapDto>()
            .ForMember(d => d.RowVersion, o => o.MapFrom(s => Convert.ToBase64String(s.RowVersion)));

        CreateMap<Swap, SwapListItemDto>()
            .ForMember(d => d.RowVersion, o => o.MapFrom(s => Convert.ToBase64String(s.RowVersion)));
    }
}
