using AutoMapper;
using QPQ.Moderation.Bll.DTOs;
using QPQ.Moderation.Domain;

namespace QPQ.Moderation.Bll.Mapping;

public class ModerationProfile : Profile
{
    public ModerationProfile()
    {
        CreateMap<Report, ReportDto>();
    }
}