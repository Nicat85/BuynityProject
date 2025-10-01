using OnlineShppingSystem.Application.Shared;
using OnlineSohppingSystem.Application.DTOs.Payment;

namespace OnlineSohppingSystem.Application.Abstracts.Services;

public interface IPaymentService
{
    Task<BaseResponse<CreatePaymentResultDto>> CreatePaymentAsync(CreatePaymentRequestDto request, CancellationToken ct = default);
    Task<BaseResponse<RefundPaymentResultDto>> RefundPaymentAsync(RefundPaymentRequestDto request, CancellationToken ct = default);
}
