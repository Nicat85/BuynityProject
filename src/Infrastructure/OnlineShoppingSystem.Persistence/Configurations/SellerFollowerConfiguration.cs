using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Persistence.Configurations;

public class SellerFollowerConfiguration : IEntityTypeConfiguration<SellerFollower>
{
    public void Configure(EntityTypeBuilder<SellerFollower> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Seller)
               .WithMany()
               .HasForeignKey(x => x.SellerId)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false); 

        builder.HasOne(x => x.Buyer)
               .WithMany()
               .HasForeignKey(x => x.BuyerId)
               .OnDelete(DeleteBehavior.Cascade)
               .IsRequired(false); 

        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
