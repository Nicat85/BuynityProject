using OnlineShppingSystem.Application.Shared;
using OnlineSohppingSystem.Application.DTOs.Order;

namespace OnlineSohppingSystem.Application.Abstracts.Services;

public interface IOrderService
{
    Task<BaseResponse<OrderResultDto>> CreateOrderAsync(OrderCreateDto request, CancellationToken ct = default);
    Task<BaseResponse<OrderResultDto>> GetByIdAsync(Guid orderId, CancellationToken ct = default);
    Task<BaseResponse<List<OrderResultDto>>> GetMyOrdersAsync(CancellationToken ct = default);
    Task<BaseResponse<bool>> MarkAsPaidAsync(Guid orderId, string providerPaymentId, CancellationToken ct = default);
    Task<BaseResponse<OrderTrackingDto>> GetTrackingAsync(Guid orderId, CancellationToken ct = default);
    Task<BaseResponse<List<OrderResultDto>>> GetAllOrdersAsync(CancellationToken ct);

}
