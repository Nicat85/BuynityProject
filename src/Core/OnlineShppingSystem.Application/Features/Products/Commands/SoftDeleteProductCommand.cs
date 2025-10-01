using MediatR;
using OnlineShppingSystem.Application.Shared;
using OnlineSohppingSystem.Application.Shared;

namespace OnlineSohppingSystem.Application.Features.Products.Commands;

public class SoftDeleteProductCommand : IRequest<BaseResponse<bool>>
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
}