using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineShppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Persistence.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        
        builder.HasQueryFilter(u => !u.IsDeleted);

        builder.Property(u => u.FullName)
               .HasMaxLength(200);

        builder.Property(u => u.Bio)
               .HasMaxLength(500);

        builder.Property(u => u.Address)
               .HasMaxLength(500);

        builder.Property(u => u.FinCode)
               .HasMaxLength(7)
               .IsFixedLength();

        builder.Property(u => u.ProfilePicture)
               .HasMaxLength(500);

        builder.Property(u => u.AvatarText)
               .HasMaxLength(2);

        builder.HasIndex(u => u.Email);
        builder.HasIndex(u => u.IsDeleted);

        builder.HasMany(u => u.Products)
               .WithOne(p => p.User)
               .HasForeignKey(p => p.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.Orders)
               .WithOne(o => o.Buyer)
               .HasForeignKey(o => o.BuyerId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}