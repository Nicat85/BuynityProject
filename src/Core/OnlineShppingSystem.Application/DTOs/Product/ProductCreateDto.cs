using Microsoft.AspNetCore.Http;
using OnlineShppingSystem.Domain.Enums;

namespace OnlineSohppingSystem.Application.DTOs.Product;

public class ProductCreateDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public int StockQuantity { get; set; }
    public bool IsSecondHand { get; set; }
    public bool IsFromStore { get; set; } 
    public ProductCondition Condition { get; set; }

    public Guid CategoryId { get; set; }
    public List<IFormFile> Images { get; set; } = new();
}

