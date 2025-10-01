namespace OnlineShppingSystem.Domain.Entities;

public class Review : BaseEntity
{
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public int Rating { get; set; }
    public string Comment { get; set; } = null!;
}
