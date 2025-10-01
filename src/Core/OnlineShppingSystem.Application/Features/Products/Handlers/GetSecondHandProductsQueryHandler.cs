using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OnlineShoppingSystem.Application.Abstracts.Repositories;
using OnlineShppingSystem.Application.Shared;
using OnlineSohppingSystem.Application.DTOs.Product;
using OnlineSohppingSystem.Application.Features.Products.Queries;
using OnlineSohppingSystem.Domain.Enums;

namespace OnlineSohppingSystem.Application.Features.Products.Handlers;

public class GetSecondHandProductsQueryHandler : IRequestHandler<GetSecondHandProductsQuery, BaseResponse<List<ProductResultDto>>>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public GetSecondHandProductsQueryHandler(IProductRepository productRepository, IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<BaseResponse<List<ProductResultDto>>> Handle(GetSecondHandProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _productRepository
            .GetQueryable()
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .Include(p => p.User)
            .Where(p => p.IsSecondHand && p.Status == ProductStatus.Active)
            .ToListAsync();

        var resultDtos = _mapper.Map<List<ProductResultDto>>(products);
        return BaseResponse<List<ProductResultDto>>.CreateSuccess(resultDtos, "2-ci əl məhsullar uğurla gətirildi");
    }
}
