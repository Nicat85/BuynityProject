using AutoMapper;
using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Application.DTOs.Product;
using OnlineSohppingSystem.Application.Models.Elasticsearch;
using OnlineSohppingSystem.Application.Features.Products.Commands;
using System;
using System.Linq;

namespace OnlineShppingSystem.Application.Mappers
{
    public class ProductMappingProfile : Profile
    {
        public ProductMappingProfile()
        {
            
            CreateMap<Product, ProductResultDto>()
               .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
               .ForMember(dest => dest.SellerName, opt => opt.MapFrom(src => src.User.FullName ?? src.User.UserName ?? "Unknown"))
               .ForMember(dest => dest.SellerId, opt => opt.MapFrom(src => src.UserId))
               .ForMember(dest => dest.DiscountPercentage, opt => opt.MapFrom(src =>
                    src.OriginalPrice.HasValue && src.OriginalPrice > 0
                        ? Math.Round((double)((src.OriginalPrice.Value - src.Price) / src.OriginalPrice.Value) * 100, 2)
                        : 0))
               .ForMember(dest => dest.HighlightedName, opt => opt.Ignore())
               .ForMember(dest => dest.ProductImages, opt => opt.MapFrom(src => src.ProductImages))
               
               .ForMember(dest => dest.MainImageUrl,
                          opt => opt.MapFrom(src =>
                              src.ProductImages
                                 .OrderBy(pi => pi.Id)
                                 .Select(pi => pi.Url)
                                 .FirstOrDefault()));

            
            CreateMap<ProductImage, ProductImageDto>();


            CreateMap<ProductCreateDto, Product>()
                .ForMember(dest => dest.ProductImages, opt => opt.Ignore());

            CreateMap<ProductUpdateDto, Product>()
                .ForMember(dest => dest.ProductImages, opt => opt.Ignore());

            
            CreateMap<Product, ProductIndexModel>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
                
                .ForMember(dest => dest.ImageUrls,
                           opt => opt.MapFrom(src =>
                               src.ProductImages
                                  .OrderBy(pi => pi.Id)
                                  .Select(pi => pi.Url)
                                  .ToList()))
                .ForMember(dest => dest.SellerId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.SellerName, opt => opt.MapFrom(src => src.User.FullName ?? src.User.UserName ?? "Unknown"));

            
            CreateMap<CreateProductCommand, Product>()
                .ForMember(dest => dest.ProductImages, opt => opt.Ignore()); 

            CreateMap<UpdateProductCommand, Product>()
                .ForMember(dest => dest.ProductImages, opt => opt.Ignore());
        }
    }
}
