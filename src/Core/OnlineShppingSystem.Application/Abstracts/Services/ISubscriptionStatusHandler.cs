using System;
using System.Threading.Tasks;

namespace OnlineShppingSystem.Application.Abstracts.Services;

public interface ISubscriptionStatusHandler
{
    Task OnSubscriptionActivatedAsync(Guid userId, string planCode, DateTime? currentPeriodEndUtc);
    Task OnSubscriptionCanceledAsync(Guid userId, string planCode);
    Task OnSubscriptionExpiredAsync(Guid userId, string planCode);
    Task OnPaymentFailedAsync(Guid userId, string planCode);
}
