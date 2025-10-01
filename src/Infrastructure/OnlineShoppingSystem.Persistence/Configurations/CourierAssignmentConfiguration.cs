using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineShppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Persistence.Configurations;

public class CourierAssignmentConfiguration : IEntityTypeConfiguration<CourierAssignment>
{
    public void Configure(EntityTypeBuilder<CourierAssignment> b)
    {
        b.HasQueryFilter(x => !x.IsDeleted);

        b.HasOne(x => x.Order)
         .WithOne(o => o.CourierAssignment)
         .HasForeignKey<CourierAssignment>(x => x.OrderId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Courier)
         .WithMany(u => u.CourierAssignments)
         .HasForeignKey(x => x.CourierId)
         .OnDelete(DeleteBehavior.Restrict);
    }
}

