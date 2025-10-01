using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OnlineShoppingSystem.Application.Abstracts.Repositories;
using OnlineShppingSystem.Application.Shared;
using OnlineSohppingSystem.Application.DTOs.Product;
using OnlineSohppingSystem.Application.Features.Products.Queries;
using OnlineSohppingSystem.Domain.Enums;
using System.Net;

namespace OnlineSohppingSystem.Application.Features.Products.Handlers;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, BaseResponse<ProductResultDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public GetProductByIdQueryHandler(IProductRepository productRepository, IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<BaseResponse<ProductResultDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _productRepository
            .GetQueryable()
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.UserId == request.UserId && p.Status != ProductStatus.Deleted);

        if (product == null)
            return BaseResponse<ProductResultDto>.Fail("Product not found", HttpStatusCode.NotFound);

        var resultDto = _mapper.Map<ProductResultDto>(product);
        return BaseResponse<ProductResultDto>.CreateSuccess(resultDto, "Product detail successfully loaded");
    }
}