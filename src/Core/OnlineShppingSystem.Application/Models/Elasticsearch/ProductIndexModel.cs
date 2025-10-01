namespace OnlineSohppingSystem.Application.Models.Elasticsearch;

public class ProductIndexModel
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
    public List<string> ImageUrls { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public Guid SellerId { get; set; }
    public string? SellerName { get; set; }
    public string? MainImageUrl => ImageUrls?.FirstOrDefault();
}
