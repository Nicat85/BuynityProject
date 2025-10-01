using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Persistence.Contexts;

public class OnlineShoppingSystemDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public OnlineShoppingSystemDbContext(DbContextOptions<OnlineShoppingSystemDbContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Favorite> Favorites { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<SellerFollower> SellerFollowers { get; set; }
    public DbSet<StoreSellerSubscription> StoreSellerSubscriptions { get; set; }
    public DbSet<SupportChatThread> SupportChatThreads { get; set; } = default!;
    public DbSet<SupportChatMessage> SupportChatMessages { get; set; } = default!;
    public DbSet<CourierAssignment> courierAssignments { get; set; } = default!;



    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(OnlineShoppingSystemDbContext).Assembly);

    }
}
