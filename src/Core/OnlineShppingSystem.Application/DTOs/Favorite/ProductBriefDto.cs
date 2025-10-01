namespace OnlineSohppingSystem.Application.DTOs.Favorite;

public sealed class ProductBriefDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public bool IsSecondHand { get; set; }
    public bool IsFromStore { get; set; }
    public string? MainImageUrl { get; set; }
    public Guid SellerId { get; set; }
    public string? SellerName { get; set; }
}
