using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineShppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Persistence.Configurations
{
    public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> b)
        {
            b.HasQueryFilter(x => !x.IsDeleted);
            
            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.Price).HasColumnType("decimal(18,2)");
            b.Property(x => x.OriginalPrice).HasColumnType("decimal(18,2)");
            
            b.HasOne(x => x.Category)
             .WithMany(c => c.Products)
             .HasForeignKey(x => x.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
           
            
            b.HasOne(x => x.User)
             .WithMany(u => u.Products)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Restrict);
            
            b.HasMany(x => x.ProductImages)
             .WithOne(pi => pi.Product)
             .HasForeignKey(pi => pi.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
             
            b.HasMany(x => x.OrderItems)
             .WithOne(oi => oi.Product)
             .HasForeignKey(oi => oi.ProductId)
             .OnDelete(DeleteBehavior.Restrict);
            
            b.HasIndex(x => x.CategoryId);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => new { x.Status, x.IsSecondHand, x.IsFromStore });
        }
    }
}