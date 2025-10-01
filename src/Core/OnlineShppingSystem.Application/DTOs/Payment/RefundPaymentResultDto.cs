namespace OnlineSohppingSystem.Application.DTOs.Payment;

public sealed class RefundPaymentResultDto
{
    public string RefundId { get; set; } = default!;
    public string Status { get; set; } = default!;
}