using MediatR;
using OnlineShppingSystem.Application.Shared;
using OnlineSohppingSystem.Application.DTOs.Product;
using OnlineSohppingSystem.Application.Shared;

namespace OnlineSohppingSystem.Application.Features.Products.Queries;

public class SearchProductsQuery : IRequest<BaseResponse<PagedResponse<ProductResultDto>>>
{
    public ProductFilterDto Filter { get; set; } = null!;
}