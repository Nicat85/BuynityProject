using OnlineSohppingSystem.Application.Events;

namespace OnlineSohppingSystem.Application.Abstracts.Services;

public interface IPaymentSubscriptionService
{
    Task<string> CreateMonthlySubscriptionCheckoutAsync(Guid userId, string planCode, string successUrl, string cancelUrl);
    Task<PaymentEvent> ParseAndValidateWebhookAsync(string payload, IDictionary<string, string> headers);
}