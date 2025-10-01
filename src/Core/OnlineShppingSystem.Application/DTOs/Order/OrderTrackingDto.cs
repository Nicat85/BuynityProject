namespace OnlineSohppingSystem.Application.DTOs.Order;

public sealed class OrderTrackingDto
{
    public Guid OrderId { get; set; }
    public string Status { get; set; } = "Pending"; 
    public int StatusCode { get; set; }             
    public DateTime PlacedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? CourierName { get; set; }
}

