namespace OnlineSohppingSystem.Application.DTOs.Order;


using OnlineSohppingSystem.Domain.Enums;

public record OrderCreateDto
{
    public List<OrderItemCreateDto> Items { get; init; } = new();
    public PaymentMethod PaymentMethod { get; init; } = PaymentMethod.Cash; 
}

