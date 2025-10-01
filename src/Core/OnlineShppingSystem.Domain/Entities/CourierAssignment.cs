using OnlineSohppingSystem.Domain.Enums;

namespace OnlineShppingSystem.Domain.Entities;

public class CourierAssignment : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public Guid CourierId { get; set; }
    public AppUser Courier { get; set; } = null!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PickedUpAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public CourierAssignmentStatus Status { get; set; } = CourierAssignmentStatus.Assigned;
}

