using AutoMapper;
using Sessions.Bll.Dtos;
using Sessions.Domain.Models;

namespace Sessions.Bll.Mapping;

public class SessionProfile : Profile
{
    public SessionProfile()
    {
        CreateMap<Skill, SkillDto>();
        CreateMap<SessionOutcome, SessionOutcomeDto>();

        CreateMap<SessionSkill, SessionSkillDto>()
            .ForMember(d => d.SkillName, o => o.MapFrom(s => s.Skill.Name));

        CreateMap<SessionSkill, SkillSessionDto>()
            .ForMember(d => d.SwapId, o => o.MapFrom(s => s.Session.SwapId))
            .ForMember(d => d.FormatCode, o => o.MapFrom(s => s.Session.Format.Code))
            .ForMember(d => d.ScheduledAt, o => o.MapFrom(s => s.Session.ScheduledAt))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Session.Status));

        CreateMap<Session, SessionDto>()
            .ForMember(d => d.FormatCode, o => o.MapFrom(s => s.Format.Code))
            .ForMember(d => d.RowVersion, o => o.MapFrom(s => Convert.ToBase64String(s.RowVersion)))
            .ForMember(d => d.Skills, o => o.MapFrom(s => s.Skills.OrderBy(ss => ss.SkillId)));

        CreateMap<Session, SessionListItemDto>()
            .ForMember(d => d.FormatCode, o => o.MapFrom(s => s.Format.Code))
            .ForMember(d => d.RowVersion, o => o.MapFrom(s => Convert.ToBase64String(s.RowVersion)));

        CreateMap<AcceptedSwap, AcceptedSwapSummaryDto>();

        CreateMap<AcceptedSwap, AcceptedSwapDto>()
            .ForMember(d => d.Sessions, o => o.MapFrom(s => s.Sessions.OrderBy(x => x.ScheduledAt)));
    }
}
