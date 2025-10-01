using MediatR;
using OnlineShppingSystem.Application.Shared;
using OnlineSohppingSystem.Application.DTOs.Product;
using OnlineSohppingSystem.Application.Shared;

namespace OnlineSohppingSystem.Application.Features.Products.Queries;

public class GetProductByIdQuery : IRequest<BaseResponse<ProductResultDto>>
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
}