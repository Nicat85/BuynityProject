namespace OnlineSohppingSystem.Application.Abstracts.Services;

public interface IStoreSellerSubscriptionCleanupService
{
    Task CleanExpiredSubscriptionsAsync();
}
