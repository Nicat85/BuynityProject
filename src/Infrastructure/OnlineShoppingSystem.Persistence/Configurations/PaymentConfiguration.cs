using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentEntity = OnlineSohppingSystem.Domain.Entities.Payment; 

namespace OnlineShoppingSystem.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<PaymentEntity>
{
    public void Configure(EntityTypeBuilder<PaymentEntity> b)
    {
       
        b.HasOne(p => p.Order)
         .WithOne(o => o.Payment)
         .HasForeignKey<PaymentEntity>(p => p.OrderId)
         .OnDelete(DeleteBehavior.Cascade);

       
        b.Property(p => p.Amount).HasColumnType("decimal(18,2)");

        
        b.HasQueryFilter(p => !p.IsDeleted);
    }
}
