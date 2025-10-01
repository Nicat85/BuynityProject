using OnlineShppingSystem.Application.Shared;
using OnlineSohppingSystem.Application.DTOs.Courier; 
using OnlineSohppingSystem.Domain.Enums;

namespace OnlineSohppingSystem.Application.Abstracts.Services
{
    public interface ICourierService
    {
        Task<BaseResponse> AssignRandomCourierAsync(Guid orderId, CancellationToken ct = default);

        Task<BaseResponse> UpdateOrderDeliveryStatusAsync(
            Guid orderId,
            CourierAssignmentStatus status,
            CancellationToken ct = default);

        Task<BaseResponse<List<CourierOrderDto>>> GetMyAssignedOrdersAsync(CancellationToken ct = default);
    }
}
