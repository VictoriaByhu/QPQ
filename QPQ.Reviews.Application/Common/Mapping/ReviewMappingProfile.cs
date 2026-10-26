using AutoMapper;
using QPQ.Reviews.Domain.Entities;
using QPQ.Reviews.Application.Reviews.Queries.GetReviewById;

namespace QPQ.Reviews.Application.Common.Mappings;

public class ReviewMappingProfile : Profile
{
    public ReviewMappingProfile()
    {
        CreateMap<Review, ReviewDto>()
            .ForMember(dest => dest.AuthorUserId, opt => opt.MapFrom(src => src.Author.UserId))
            .ForMember(dest => dest.AuthorUsername, opt => opt.MapFrom(src => src.Author.Username));
    }
}