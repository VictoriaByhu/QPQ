using AutoMapper;
using QPQ.Moderation.Bll.DTOs;
using QPQ.Moderation.Domain;
using QPQ.Moderation.Domain.Entities;

namespace QPQ.Moderation.Bll.Mapping;

public class ModerationProfile : Profile
{
    public ModerationProfile()
    {
        // Кажемо маперу, як перетворювати ці класи туди й назад
        CreateMap<Report, ReportDto>();
        CreateMap<CreateReportDto, Report>();
    }
}