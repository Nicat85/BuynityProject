using OnlineShppingSystem.Application.Shared;

namespace OnlineSohppingSystem.Application.Abstracts.Services;

public interface ICheckoutService
{
    Task<BaseResponse<string>> CreateOrderCheckoutAsync(Guid orderId, CancellationToken ct = default);
}