using AutoMapper;
using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Application.DTOs.Review;

namespace OnlineSohppingSystem.Application.Mapping.Profiles;

public sealed class ReviewMappingProfile : Profile
{
    public ReviewMappingProfile()
    {
        CreateMap<Review, ReviewResultDto>()
            .ForMember(d => d.UserFullName, opt => opt.MapFrom(s => s.User.FullName ?? s.User.UserName ?? ""))
            .ReverseMap();
    }
}
