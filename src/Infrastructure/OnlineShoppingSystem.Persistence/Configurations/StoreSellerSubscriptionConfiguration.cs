using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Persistence.Configurations
{
    public class StoreSellerSubscriptionConfiguration : IEntityTypeConfiguration<StoreSellerSubscription>
    {
        public void Configure(EntityTypeBuilder<StoreSellerSubscription> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Provider)
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(x => x.ExternalCustomerId)
                   .HasMaxLength(100);

            builder.Property(x => x.ExternalSubscriptionId)
                   .HasMaxLength(100);

            builder.Property(x => x.Currency)
                   .HasMaxLength(10);

            builder.HasOne(x => x.User)
                   .WithMany()                      
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Restrict) 
                   .IsRequired();                     

            builder.HasIndex(x => new { x.UserId, x.Status });

           
            builder.HasQueryFilter(s => s.User != null && !s.User.IsDeleted);

           
        }
    }
}
