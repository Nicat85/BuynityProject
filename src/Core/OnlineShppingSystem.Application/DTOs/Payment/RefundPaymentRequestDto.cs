using System.ComponentModel.DataAnnotations;

namespace OnlineSohppingSystem.Application.DTOs.Payment;

public sealed class RefundPaymentRequestDto
{
    [Required] public Guid OrderId { get; set; }
    [Range(0.01, double.MaxValue)] public decimal? Amount { get; set; } 
    public string? Reason { get; set; }
}
