using AutoMapper;
using OnlineShppingSystem.Application.DTOs.CategoriesDtos;
using OnlineShppingSystem.Domain.Entities;

namespace OnlineShppingSystem.Application.Mappers;

public class CategoryMapper : Profile
{
    public CategoryMapper()
    {
        CreateMap<CreateCategoryDto, Category>();
        CreateMap<UpdateCategoryDto, Category>();
        CreateMap<Category, CategoryResultDto>()
            .ForMember(dest => dest.Children, opt => opt.MapFrom(src => src.Children.Where(c => !c.IsDeleted)));
    }
}

