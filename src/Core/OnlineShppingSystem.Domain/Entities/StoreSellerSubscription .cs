using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Domain.Enums;

namespace OnlineSohppingSystem.Domain.Entities;


public class StoreSellerSubscription : BaseEntity
{
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = default!;

    public string Provider { get; set; } = "CardGateway"; 
    public string ExternalCustomerId { get; set; } = string.Empty;
    public string ExternalSubscriptionId { get; set; } = string.Empty;

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Pending;

    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public bool AutoRenew { get; set; } = true;

    public string Currency { get; set; } = "AZN";
    public long UnitAmountMinor { get; set; } 
}