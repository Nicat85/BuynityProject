using System.Text.Json.Serialization;

namespace OnlineSohppingSystem.Application.DTOs.Product;

public class ProductResultDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public int StockQuantity { get; set; }
    public bool IsSecondHand { get; set; }
    public bool IsFromStore { get; set; }
    public int Condition { get; set; }
    public int Status { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public List<ProductImageDto> ProductImages { get; set; } = new();

    public string? MainImageUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid SellerId { get; set; }                 
    public string? SellerName { get; set; }
    public double DiscountPercentage { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HighlightedName { get; set; }
}
