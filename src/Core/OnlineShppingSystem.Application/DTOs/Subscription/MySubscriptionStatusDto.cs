namespace OnlineSohppingSystem.Application.DTOs.Subscription;

public sealed class MySubscriptionStatusDto
{
    public bool IsActive { get; set; }
    public string Status { get; set; } = "None"; 
    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
}
