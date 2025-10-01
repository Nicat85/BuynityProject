using System;
using OnlineSohppingSystem.Domain.Enums;

namespace OnlineSohppingSystem.Application.DTOs.Courier;

public sealed record CourierOrderDto
{
    public Guid OrderId { get; init; }
    public OrderStatus OrderStatus { get; init; }
    public decimal TotalPrice { get; init; }
    public string BuyerFullName { get; init; } = string.Empty;
    public string BuyerAddress { get; init; } = string.Empty;
    public string SellerName { get; init; } = string.Empty;
    public string SellerAddress { get; init; } = string.Empty;
    public DateTime AssignedAt { get; init; }
}
