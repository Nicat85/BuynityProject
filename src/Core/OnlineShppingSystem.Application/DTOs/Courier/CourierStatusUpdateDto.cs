using OnlineSohppingSystem.Domain.Enums;

namespace OnlineSohppingSystem.Application.DTOs.Courier;

public sealed class CourierStatusUpdateDto
{
    public CourierAssignmentStatus Status { get; set; }
}
