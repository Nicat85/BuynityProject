namespace OnlineSohppingSystem.Application.DTOs.Order;

public record OrderItemCreateDto
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
}
