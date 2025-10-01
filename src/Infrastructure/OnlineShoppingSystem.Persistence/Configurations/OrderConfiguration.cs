using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineShppingSystem.Domain.Entities; 
using PaymentEntity = OnlineSohppingSystem.Domain.Entities.Payment; 

namespace OnlineShoppingSystem.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        
        b.HasOne(o => o.Buyer)
         .WithMany(u => u.Orders)
         .HasForeignKey(o => o.BuyerId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(o => o.Payment)
         .WithOne(p => p.Order)
         .HasForeignKey<PaymentEntity>(p => p.OrderId)
         .OnDelete(DeleteBehavior.Cascade);

        
        b.Property(o => o.TotalPrice).HasColumnType("decimal(18,2)");

       
        b.HasQueryFilter(o => !o.IsDeleted && !o.Buyer.IsDeleted);


        b.HasIndex(o => new { o.BuyerId, o.OrderDate });
    }
}
