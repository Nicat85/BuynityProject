using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderItemEntity = OnlineSohppingSystem.Domain.Entities.OrderItem; 

namespace OnlineShoppingSystem.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItemEntity>
{
    public void Configure(EntityTypeBuilder<OrderItemEntity> b)
    {
       
        b.HasOne(oi => oi.Order)
         .WithMany(o => o.OrderItems)
         .HasForeignKey(oi => oi.OrderId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(oi => oi.Product)
         .WithMany(p => p.OrderItems)
         .HasForeignKey(oi => oi.ProductId)
         .OnDelete(DeleteBehavior.Restrict);

        
        b.Property(oi => oi.UnitPrice).HasColumnType("decimal(18,2)");

       
        b.HasQueryFilter(oi => !oi.IsDeleted && !oi.Product.IsDeleted);

       
        b.HasIndex(oi => new { oi.OrderId, oi.ProductId });
    }
}
