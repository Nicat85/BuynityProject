using OnlineShppingSystem.Domain.Entities;

namespace OnlineSohppingSystem.Domain.Entities;

public class SellerFollower : BaseEntity
{
    public Guid SellerId { get; set; }
    public AppUser Seller { get; set; } = null!;

    public Guid BuyerId { get; set; }
    public AppUser Buyer { get; set; } = null!;
}


