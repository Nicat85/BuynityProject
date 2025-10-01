using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineSohppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Persistence.Configurations
{
    public class SupportChatThreadConfiguration : IEntityTypeConfiguration<SupportChatThread>
    {
        public void Configure(EntityTypeBuilder<SupportChatThread> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Subject)
                   .HasMaxLength(200)
                   .IsRequired();

            
            builder.HasOne(x => x.Customer)
                   .WithMany()
                   .HasForeignKey(x => x.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);

            builder.HasOne(x => x.AssignedTo)
                   .WithMany()
                   .HasForeignKey(x => x.AssignedToId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false); 

            builder.Property(x => x.LastMessageAt)
                   .HasDefaultValueSql("GETUTCDATE()");

            builder.HasIndex(x => new { x.Status, x.LastMessageAt });

            
        }
    }
}
