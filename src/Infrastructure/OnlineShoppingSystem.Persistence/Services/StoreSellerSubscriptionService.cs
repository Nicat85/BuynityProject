using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnlineShoppingSystem.Persistence.Contexts;
using OnlineShppingSystem.Application.Shared;
using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.DTOs.Subscription;
using OnlineSohppingSystem.Application.Shared.Settings;
using OnlineSohppingSystem.Domain.Entities;
using OnlineSohppingSystem.Domain.Enums;
using Stripe;
using Stripe.Checkout;

namespace OnlineShoppingSystem.Persistence.Services
{
    public sealed class StoreSellerSubscriptionService : IStoreSellerSubscriptionService
    {
        private readonly OnlineShoppingSystemDbContext _db;
        private readonly StripeSettings _stripe;
        private readonly UserManager<AppUser> _userManager;

        public StoreSellerSubscriptionService(
            OnlineShoppingSystemDbContext db,
            IOptions<StripeSettings> stripe,
            UserManager<AppUser> userManager)
        {
            _db = db;
            _stripe = stripe.Value ?? throw new ArgumentNullException(nameof(stripe));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            StripeConfiguration.ApiKey = _stripe.ApiKey;
        }

        private static bool IsActive(StoreSellerSubscription? sub, DateTime utcNow)
        {
            if (sub is null) return false;
            if (sub.Status != SubscriptionStatus.Active) return false;
            if (!sub.CurrentPeriodEnd.HasValue) return false;

            var skew = TimeSpan.FromMinutes(5);
            var endUtc = DateTime.SpecifyKind(sub.CurrentPeriodEnd.Value, DateTimeKind.Utc);
            return endUtc.Add(skew) > utcNow;
        }

        public async Task<bool> HasActiveAsync(Guid userId, string planCode, CancellationToken ct = default)
        {
            var utcNow = DateTime.UtcNow;

         
            var sub = await _db.Set<StoreSellerSubscription>()
                .AsNoTracking()
                .Where(s => s.UserId == userId && !s.IsDeleted)
                .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (IsActive(sub, utcNow))
                return true;

        
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user != null && await _userManager.IsInRoleAsync(user, "StoreSeller"))
                return true;

            return false;
        }

        public async Task<BaseResponse<StartStoreSellerSubscriptionResponse>> StartAsync(Guid userId, StartStoreSellerSubscriptionRequest req, CancellationToken ct = default)
        {
            var planCode = string.IsNullOrWhiteSpace(req.PlanCode) ? "store_seller_monthly" : req.PlanCode;

            if (await HasActiveAsync(userId, planCode, ct))
                return BaseResponse<StartStoreSellerSubscriptionResponse>.Fail("Plan artıq aktivdir. Ödəniş linki yaradılmadı.", HttpStatusCode.Conflict);

            if (_stripe.Plans is null || !_stripe.Plans.TryGetValue(planCode, out var plan) || string.IsNullOrWhiteSpace(plan.PriceId))
                return BaseResponse<StartStoreSellerSubscriptionResponse>.Fail("Plan konfiqurasiyası tapılmadı.", HttpStatusCode.BadRequest);

            var successUrl = (_stripe.SuccessUrl ?? "https://localhost:7237/billing/success?plan={PLAN_CODE}")
                             .Replace("{PLAN_CODE}", Uri.EscapeDataString(planCode));
            var cancelUrl = (_stripe.CancelUrl ?? "https://localhost:7237/billing/cancel?plan={PLAN_CODE}")
                            .Replace("{PLAN_CODE}", Uri.EscapeDataString(planCode));

            if (!string.IsNullOrWhiteSpace(req.SuccessUrl)) successUrl = req.SuccessUrl!;
            if (!string.IsNullOrWhiteSpace(req.CancelUrl)) cancelUrl = req.CancelUrl!;

            var options = new SessionCreateOptions
            {
                Mode = "subscription",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions { Quantity = 1, Price = plan.PriceId }
                },
                Metadata = new Dictionary<string, string>
                {
                    ["userId"] = userId.ToString(),
                    ["planCode"] = planCode,
                    ["grantRole"] = "StoreSeller"
                }
            };

            try
            {
                var session = await new SessionService().CreateAsync(options, cancellationToken: ct);

                var dto = new StartStoreSellerSubscriptionResponse
                {
                    CheckoutUrl = session.Url!
                };

                return BaseResponse<StartStoreSellerSubscriptionResponse>.CreateSuccess(
                    dto,
                    "Subscription checkout link yaradıldı.",
                    HttpStatusCode.OK);
            }
            catch (StripeException se)
            {
                return BaseResponse<StartStoreSellerSubscriptionResponse>.Fail($"Stripe error: {se.Message}", HttpStatusCode.BadGateway);
            }
            catch (Exception ex)
            {
                return BaseResponse<StartStoreSellerSubscriptionResponse>.Fail($"Error: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<BaseResponse<MySubscriptionStatusDto>> GetMyStatusAsync(Guid userId, CancellationToken ct = default)
        {
            var utcNow = DateTime.UtcNow;

            var sub = await _db.Set<StoreSellerSubscription>()
                .AsNoTracking()
                .Where(s => s.UserId == userId && !s.IsDeleted)
                .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
                .FirstOrDefaultAsync(ct);

            var isActive = IsActive(sub, utcNow);

           
            if (!isActive)
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user != null && await _userManager.IsInRoleAsync(user, "StoreSeller"))
                    isActive = true;
            }

            var dto = new MySubscriptionStatusDto
            {
                IsActive = isActive,
                Status = sub?.Status.ToString() ?? (isActive ? "Active" : "None"),
                CurrentPeriodStart = sub?.CurrentPeriodStart,
                CurrentPeriodEnd = sub?.CurrentPeriodEnd
            };

            return BaseResponse<MySubscriptionStatusDto>.CreateSuccess(dto, "Success.");
        }

      
        public async Task<BaseResponse<bool>> HandleWebhookAsync(string jsonPayload, IDictionary<string, string> headers, CancellationToken ct = default)
        {
           
            return await Task.FromResult(BaseResponse<bool>.CreateSuccess(true, "Handled by StripeWebhookController."));
        }
    }
}
