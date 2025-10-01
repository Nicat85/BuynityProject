namespace OnlineSohppingSystem.Application.Events;

public sealed class PaymentEvent
{
    public string Type { get; set; } = string.Empty; 
    public string ExternalCustomerId { get; set; } = string.Empty;
    public string ExternalSubscriptionId { get; set; } = string.Empty;
    public long? UnitAmountMinor { get; set; }
    public string? Currency { get; set; }
    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public Guid? InternalUserId { get; set; }

    public string? PlanCode { get; set; }
    public string? PriceId { get; set; }
}