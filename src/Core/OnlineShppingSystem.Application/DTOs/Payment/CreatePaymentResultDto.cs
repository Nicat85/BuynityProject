namespace OnlineSohppingSystem.Application.DTOs.Payment;

public sealed class CreatePaymentResultDto
{
    public string PaymentIntentId { get; set; } = default!;
    public string ClientSecret { get; set; } = default!;
    public string Status { get; set; } = default!;
}
