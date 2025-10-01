using OnlineSohppingSystem.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace OnlineSohppingSystem.Application.DTOs.Payment;

public sealed class CreatePaymentRequestDto
{
    [Required] public Guid OrderId { get; set; }
    [Range(0.01, double.MaxValue)] public decimal Amount { get; set; }
    [Required] public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Card;
    public string? CustomerEmail { get; set; }
    public string? Description { get; set; }
}