using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineSohppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Persistence.Configurations
{
    public class SupportChatMessageConfiguration : IEntityTypeConfiguration<SupportChatMessage>
    {
        public void Configure(EntityTypeBuilder<SupportChatMessage> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Text)
                   .HasMaxLength(4000);

            builder.HasOne(x => x.Thread)
                   .WithMany(t => t.Messages) 
                   .HasForeignKey(x => x.ThreadId)
                   .OnDelete(DeleteBehavior.Cascade)
                   .IsRequired(); 

           
            builder.HasOne(x => x.Sender)
                   .WithMany()
                   .HasForeignKey(x => x.SenderId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false); 

            builder.Property(x => x.CreatedAt)
                   .HasDefaultValueSql("GETUTCDATE()");

            builder.HasIndex(x => new { x.ThreadId, x.CreatedAt });

            
        }
    }
}
