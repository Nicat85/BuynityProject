namespace OnlineSohppingSystem.Application.DTOs.Favorite;

public sealed class FavoriteResultDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public DateTime CreatedAt { get; set; }

    public ProductBriefDto Product { get; set; } = new();
}
