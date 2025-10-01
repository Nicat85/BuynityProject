namespace OnlineShppingSystem.Domain.Entities;

public class ProductImage : BaseEntity
{
    public string Url { get; set; } = null!;
    public string PublicId { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product? Product { get; set; } 
}

