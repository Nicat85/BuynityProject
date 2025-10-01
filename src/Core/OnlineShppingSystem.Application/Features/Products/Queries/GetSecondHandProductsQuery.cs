using MediatR;
using OnlineShppingSystem.Application.Shared;
using OnlineSohppingSystem.Application.DTOs.Product;
using OnlineSohppingSystem.Application.Shared;

namespace OnlineSohppingSystem.Application.Features.Products.Queries;

public class GetSecondHandProductsQuery : IRequest<BaseResponse<List<ProductResultDto>>> { }