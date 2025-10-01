using OnlineShppingSystem.Domain.Enums;
using OnlineSohppingSystem.Domain.Entities;
using OnlineSohppingSystem.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineShppingSystem.Domain.Entities;

public class Product : BaseEntity
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = null!;

    [MaxLength(2000)]
    public string Description { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? OriginalPrice { get; set; }

    public int StockQuantity { get; set; }
    public ProductCondition Condition { get; set; } = ProductCondition.Good;
    public ProductStatus Status { get; set; } = ProductStatus.Active;
    public bool IsSecondHand { get; set; } = false;
    public bool IsFromStore { get; set; } = false;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

   
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

   

    public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}