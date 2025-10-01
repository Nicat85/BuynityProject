using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineShppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Persistence.Configurations
{
    public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
    {
        public void Configure(EntityTypeBuilder<Favorite> builder)
        {
            builder.ToTable("Favorites");

            builder.HasKey(f => f.Id);

            
            builder.HasIndex(f => new { f.UserId, f.ProductId })
                   .IsUnique()
                   .HasFilter("[IsDeleted] = 0");

            
            builder.HasQueryFilter(f => !f.IsDeleted);

            builder.HasOne(f => f.User)
                   .WithMany(u => u.Favorites)
                   .HasForeignKey(f => f.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(f => f.Product)
                   .WithMany(p => p.Favorites)
                   .HasForeignKey(f => f.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Property(f => f.UserId).IsRequired();
            builder.Property(f => f.ProductId).IsRequired();

            
            builder.Property(f => f.IsDeleted)
                   .HasDefaultValue(false)
                   .IsRequired();

            builder.Property(f => f.CreatedAt)
                   .HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
