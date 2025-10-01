namespace OnlineSohppingSystem.Application.DTOs.Subscription;

public sealed class SubscriptionStatusDto
{
    public string Status { get; set; } = string.Empty;
    public DateTime? CurrentPeriodEnd { get; set; }
}
