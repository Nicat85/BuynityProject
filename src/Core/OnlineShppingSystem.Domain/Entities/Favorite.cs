using OnlineShppingSystem.Domain.Entities;

namespace OnlineShppingSystem.Domain.Entities;

public class Favorite : BaseEntity
{
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
}
