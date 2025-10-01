namespace OnlineSohppingSystem.Application.DTOs.Subscription;

public sealed class StartStoreSellerSubscriptionRequest
{
    public string PlanCode { get; set; } = "store_seller_monthly"; 
    public string? SuccessUrl { get; set; }
    public string? CancelUrl { get; set; }
}
