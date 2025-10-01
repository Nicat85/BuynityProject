using MediatR;
using Microsoft.AspNetCore.Http;
using OnlineShppingSystem.Application.Shared;
using OnlineShppingSystem.Domain.Enums;
using OnlineSohppingSystem.Application.DTOs.Product;
using OnlineSohppingSystem.Application.Shared;
using OnlineSohppingSystem.Domain.Enums;

namespace OnlineSohppingSystem.Application.Features.Products.Commands;

public class UpdateProductCommand : IRequest<BaseResponse<ProductResultDto>>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public int StockQuantity { get; set; }
    public bool IsSecondHand { get; set; }
    public bool IsFromStore { get; set; }
    public ProductCondition Condition { get; set; }
    public Guid CategoryId { get; set; }
    public List<IFormFile>? Images { get; set; }
    public List<Guid>? DeleteImageIds { get; set; }
}