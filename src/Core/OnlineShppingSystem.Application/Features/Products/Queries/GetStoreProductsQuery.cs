using MediatR;
using OnlineShppingSystem.Application.Shared;
using OnlineSohppingSystem.Application.DTOs.Product;
using OnlineSohppingSystem.Application.Shared;

namespace OnlineSohppingSystem.Application.Features.Products.Queries;

public class GetStoreProductsQuery : IRequest<BaseResponse<List<ProductResultDto>>> { }