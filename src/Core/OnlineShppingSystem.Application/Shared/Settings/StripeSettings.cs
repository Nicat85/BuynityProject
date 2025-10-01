namespace OnlineSohppingSystem.Application.Shared.Settings;

public sealed class StripeSettings
{
    public string ApiKey { get; set; } = default!;
    public string WebhookSecret { get; set; } = default!;
    public string Currency { get; set; } = "AZN";
    public string? SuccessUrl { get; set; }  
    public string? CancelUrl { get; set; }

    public Dictionary<string, StripePlan> Plans { get; set; } = new();
}