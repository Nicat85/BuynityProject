using OnlineSohppingSystem.Domain.Enums;

namespace OnlineSohppingSystem.Application.DTOs.Order;

public record OrderResultDto
{
    public Guid OrderId { get; init; }
    public string? BuyerName { get; init; }
    public string? BuyerPhone { get; init; }
    public string? BuyerAddress { get; init; }

    public DateTime OrderDate { get; init; }
    public decimal TotalPrice { get; init; }
    public OrderStatus Status { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public List<OrderItemResultDto> Items { get; init; } = new();
}
