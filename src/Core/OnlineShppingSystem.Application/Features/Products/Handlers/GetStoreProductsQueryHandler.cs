using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OnlineShoppingSystem.Application.Abstracts.Repositories;
using OnlineShppingSystem.Application.Shared;
using OnlineSohppingSystem.Application.DTOs.Product;
using OnlineSohppingSystem.Application.Features.Products.Queries;
using OnlineSohppingSystem.Domain.Enums;

namespace OnlineSohppingSystem.Application.Features.Products.Handlers;

public class GetStoreProductsQueryHandler : IRequestHandler<GetStoreProductsQuery, BaseResponse<List<ProductResultDto>>>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public GetStoreProductsQueryHandler(IProductRepository productRepository, IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<BaseResponse<List<ProductResultDto>>> Handle(GetStoreProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _productRepository
            .GetQueryable()
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .Include(p => p.User)
            .Where(p => p.IsFromStore && p.Status == ProductStatus.Active)
            .ToListAsync();

        var resultDtos = _mapper.Map<List<ProductResultDto>>(products);
        return BaseResponse<List<ProductResultDto>>.CreateSuccess(resultDtos, "Mağaza məhsulları uğurla gətirildi");
    }
}
