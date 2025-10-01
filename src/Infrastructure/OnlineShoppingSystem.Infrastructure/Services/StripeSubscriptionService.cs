using Microsoft.Extensions.Options;
using OnlineSohppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.Events;
using OnlineSohppingSystem.Application.Shared.Settings;
using Stripe;
using Stripe.Checkout;

namespace OnlineShoppingSystem.Infrastructure.Services;

public sealed class StripeSubscriptionService : IPaymentSubscriptionService
{
    private readonly StripeSettings _stripe;

    public StripeSubscriptionService(IOptions<StripeSettings> stripe)
    {
        _stripe = stripe.Value ?? throw new ArgumentNullException(nameof(stripe));
        if (string.IsNullOrWhiteSpace(_stripe.ApiKey))
            throw new InvalidOperationException("Stripe ApiKey boş ola bilməz.");
        StripeConfiguration.ApiKey = _stripe.ApiKey;
    }

   
    private async Task<string> ResolveCheckoutPriceIdAsync(string priceOrProductId)
    {
        if (string.IsNullOrWhiteSpace(priceOrProductId))
            throw new InvalidOperationException("Price/Product id is empty.");

        if (priceOrProductId.StartsWith("price_", StringComparison.OrdinalIgnoreCase))
            return priceOrProductId;

        if (priceOrProductId.StartsWith("prod_", StringComparison.OrdinalIgnoreCase))
        {
            var priceService = new PriceService();
            var prices = await priceService.ListAsync(new PriceListOptions
            {
                Product = priceOrProductId,
                Active = true,
                Limit = 10
            });

            var recurring = prices.Data.FirstOrDefault(p => p.Recurring != null);
            if (recurring == null)
                throw new InvalidOperationException(
                    $"No active recurring price found for product '{priceOrProductId}'. Create a monthly price in Stripe.");

            return recurring.Id; 
        }

        throw new InvalidOperationException($"Invalid id '{priceOrProductId}'. Expecting 'price_' or 'prod_'.");
    }

    public async Task<string> CreateMonthlySubscriptionCheckoutAsync(
        Guid userId,
        string planCode,
        string successUrl,
        string cancelUrl)
    {
        if (_stripe.Plans == null ||
            !_stripe.Plans.TryGetValue(planCode, out var plan) ||
            plan == null ||
            string.IsNullOrWhiteSpace(plan.PriceId))
        {
            throw new InvalidOperationException($"Unknown planCode or missing PriceId: '{planCode}'.");
        }

        
        var priceId = await ResolveCheckoutPriceIdAsync(plan.PriceId);

        var options = new SessionCreateOptions
        {
            Mode = "subscription",
            SuccessUrl = string.IsNullOrWhiteSpace(successUrl)
                ? "https://localhost/success?session_id={CHECKOUT_SESSION_ID}"
                : successUrl,
            CancelUrl = string.IsNullOrWhiteSpace(cancelUrl)
                ? "https://localhost/cancel"
                : cancelUrl,
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions { Price = priceId, Quantity = 1 }
            },
            
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = userId.ToString(),
                ["planCode"] = planCode
            }
        };

        var sessionService = new SessionService();
        var session = await sessionService.CreateAsync(options);
        if (string.IsNullOrWhiteSpace(session?.Url))
            throw new InvalidOperationException("Stripe returned empty checkout Url.");

        return session.Url!;
    }

    public Task<PaymentEvent> ParseAndValidateWebhookAsync(string payload, IDictionary<string, string> headers)
    {
        if (headers == null) throw new ArgumentNullException(nameof(headers));

        if (string.IsNullOrWhiteSpace(_stripe.WebhookSecret))
            throw new InvalidOperationException("Stripe webhook secret is not configured.");

        if (!headers.TryGetValue("Stripe-Signature", out var sigHeader) &&
            !headers.TryGetValue("stripe-signature", out sigHeader))
            throw new InvalidOperationException("Missing Stripe-Signature header");

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json: payload,
                stripeSignatureHeader: sigHeader,
                secret: _stripe.WebhookSecret
            );
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Invalid Stripe signature: {ex.Message}");
        }

        var result = new PaymentEvent { Type = stripeEvent.Type ?? string.Empty };

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                {
                    var session = stripeEvent.Data.Object as Session;
                    result.ExternalCustomerId = session?.CustomerId ?? string.Empty;
                    result.ExternalSubscriptionId = session?.SubscriptionId ?? string.Empty;

                    if (session?.Metadata != null)
                    {
                        if (session.Metadata.TryGetValue("userId", out var uid) && Guid.TryParse(uid, out var g))
                            result.InternalUserId = g;

                        if (session.Metadata.TryGetValue("planCode", out var pc) && !string.IsNullOrWhiteSpace(pc))
                            result.PlanCode = pc;
                    }
                    break;
                }

            case "invoice.paid":
            case "invoice.payment_succeeded":
                {
                    var invoice = stripeEvent.Data.Object as Invoice;

                    result.ExternalCustomerId = invoice?.CustomerId ?? string.Empty;
                    result.ExternalSubscriptionId = TryGetInvoiceSubscriptionId(invoice);
                    result.UnitAmountMinor = invoice?.Total;
                    result.Currency = invoice?.Currency?.ToUpperInvariant();

                    var line = invoice?.Lines?.Data?.FirstOrDefault();

                    var priceObj = GetPropValue(line, "Price");
                    var priceId = priceObj != null ? GetPropValue(priceObj, "Id") as string : null;

                    if (string.IsNullOrWhiteSpace(priceId))
                    {
                        var planObj = GetPropValue(line, "Plan");
                        var planId = planObj != null ? GetPropValue(planObj, "Id") as string : null;
                       
                    }

                    if (!string.IsNullOrWhiteSpace(priceId))
                    {
                        result.PriceId = priceId;
                        if (string.IsNullOrWhiteSpace(result.PlanCode))
                            result.PlanCode = TryGetPlanCodeByPriceId(priceId);
                    }

                    var period = GetPropValue(line, "Period");
                    if (period != null)
                    {
                        var startVal = GetPropValue(period, "Start");
                        var endVal = GetPropValue(period, "End");

                        var startDt = ConvertToUtcDateTime(startVal);
                        var endDt = ConvertToUtcDateTime(endVal);

                        if (startDt.HasValue) result.CurrentPeriodStart = startDt.Value;
                        if (endDt.HasValue) result.CurrentPeriodEnd = endDt.Value;
                    }
                    break;
                }

            case "customer.subscription.created":
            case "customer.subscription.updated":
            case "customer.subscription.deleted":
                {
                    var subs = stripeEvent.Data.Object as Subscription;
                    result.ExternalCustomerId = subs?.CustomerId ?? string.Empty;
                    result.ExternalSubscriptionId = subs?.Id ?? string.Empty;

                    var (start, end) = TryGetSubscriptionPeriod(subs);
                    if (start.HasValue) result.CurrentPeriodStart = start.Value;
                    if (end.HasValue) result.CurrentPeriodEnd = end.Value;
                    break;
                }

            default:
                break;
        }

        return Task.FromResult(result);
    }

    private static string TryGetInvoiceSubscriptionId(Invoice? invoice)
    {
        if (invoice == null) return string.Empty;

        var pSubId = invoice.GetType().GetProperty("SubscriptionId");
        if (pSubId != null)
        {
            var val = pSubId.GetValue(invoice) as string;
            if (!string.IsNullOrWhiteSpace(val)) return val!;
        }

        var pSub = invoice.GetType().GetProperty("Subscription");
        if (pSub != null)
        {
            var subObj = pSub.GetValue(invoice);
            var idProp = subObj?.GetType().GetProperty("Id");
            var idVal = idProp?.GetValue(subObj) as string;
            if (!string.IsNullOrWhiteSpace(idVal)) return idVal!;
        }

        return string.Empty;
    }

    private static (DateTime? start, DateTime? end) TryGetSubscriptionPeriod(Subscription? subs)
    {
        if (subs == null) return (null, null);

        var t = subs.GetType();

        var cpsVal = t.GetProperty("CurrentPeriodStart")?.GetValue(subs);
        var cpeVal = t.GetProperty("CurrentPeriodEnd")?.GetValue(subs);

        var start = ConvertToUtcDateTime(cpsVal);
        var end = ConvertToUtcDateTime(cpeVal);

        return (start, end);
    }

    private static object? GetPropValue(object? obj, string propName)
    {
        if (obj == null) return null;
        var pi = obj.GetType().GetProperty(propName);
        return pi != null ? pi.GetValue(obj) : null;
    }

    private static DateTime? ConvertToUtcDateTime(object? value)
    {
        if (value == null) return null;

        if (value is DateTime dt)
            return dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);

        if (value is DateTimeOffset dto)
            return dto.UtcDateTime;

        if (value is long lunix)
            return DateTimeOffset.FromUnixTimeSeconds(lunix).UtcDateTime;

        if (value is int iunix)
            return DateTimeOffset.FromUnixTimeSeconds(iunix).UtcDateTime;

        if (value is string s && long.TryParse(s, out var asLong))
            return DateTimeOffset.FromUnixTimeSeconds(asLong).UtcDateTime;

        return null;
    }

   
    private string? TryGetPlanCodeByPriceId(string priceId)
    {
        if (_stripe?.Plans == null) return null;

        
        foreach (var kv in _stripe.Plans)
        {
            var configured = kv.Value?.PriceId;
            if (string.IsNullOrWhiteSpace(configured)) continue;

            if (string.Equals(configured, priceId, StringComparison.OrdinalIgnoreCase))
                return kv.Key;
        }

       
        try
        {
            var priceService = new PriceService();
            var price = priceService.Get(priceId);
            var productId = price?.ProductId;

            if (!string.IsNullOrWhiteSpace(productId))
            {
                foreach (var kv in _stripe.Plans)
                {
                    var configured = kv.Value?.PriceId;
                    if (configured != null &&
                        configured.StartsWith("prod_", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(configured, productId, StringComparison.OrdinalIgnoreCase))
                    {
                        return kv.Key;
                    }
                }
            }
        }
        catch
        {
          
        }

        return null;
    }
}
