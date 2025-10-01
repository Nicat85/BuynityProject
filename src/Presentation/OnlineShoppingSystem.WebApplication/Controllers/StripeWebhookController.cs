using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnlineShppingSystem.Application.Abstracts.Repositories;
using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.Shared.Settings;
using OnlineSohppingSystem.Domain.Entities;
using OnlineSohppingSystem.Domain.Enums;
using Stripe;
using Stripe.Checkout;
using System.IO;

namespace OnlineShppingSystem.WebAPI.Controllers;

[ApiController]
[Route("api/webhooks/stripe")]
[ApiExplorerSettings(IgnoreApi = true)]
[AllowAnonymous]
public sealed class StripeWebhookController : ControllerBase
{
    private readonly string _webhookSecret;
    private readonly IRepository<Payment> _payments;
    private readonly IRepository<Order> _orders;
    private readonly IUnitOfWork _uow;
    private readonly UserManager<AppUser> _userManager;
    private readonly ICourierService _couriers;
    private readonly OnlineShoppingSystem.Persistence.Contexts.OnlineShoppingSystemDbContext _db;

    public StripeWebhookController(
        IOptions<StripeSettings> stripe,
        IRepository<Payment> payments,
        IRepository<Order> orders,
        IUnitOfWork uow,
        UserManager<AppUser> userManager,
        ICourierService courierService,
        OnlineShoppingSystem.Persistence.Contexts.OnlineShoppingSystemDbContext db)
    {
        _webhookSecret = stripe.Value.WebhookSecret ?? throw new ArgumentNullException(nameof(StripeSettings.WebhookSecret));
        _payments = payments;
        _orders = orders;
        _uow = uow;
        _userManager = userManager;
        _couriers = courierService;
        _db = db;
    }

    [HttpPost]
    [Consumes("application/json")]
    public async Task<IActionResult> Handle()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        Event stripeEvent;
        try
        {
            var sig = Request.Headers["Stripe-Signature"];
            stripeEvent = EventUtility.ConstructEvent(json, sig, _webhookSecret);
        }
        catch (StripeException ex)
        {
            Console.WriteLine($"⚠️ Webhook signature error: {ex.Message}");
            return BadRequest();
        }

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                {
                    var session = stripeEvent.Data.Object as Session;
                    if (session != null)
                        await HandleCheckoutSessionCompleted(session);
                    break;
                }

            case "payment_intent.succeeded":
                {
                    var pi = stripeEvent.Data.Object as PaymentIntent;
                    if (pi != null)
                        await HandlePaymentIntentSucceeded(pi);
                    break;
                }

            default:
                Console.WriteLine($"Unhandled Stripe event: {stripeEvent.Type}");
                break;
        }

        return Ok();
    }

    private async Task HandleCheckoutSessionCompleted(Session session)
    {
        
        var payment = await _payments.GetAll()
            .FirstOrDefaultAsync(p => p.ProviderPaymentId == session.Id);

        if (payment != null && payment.Status != PaymentStatus.Succeeded)
        {
            payment.Status = PaymentStatus.Succeeded;
            payment.PaymentDate = DateTime.UtcNow;

            var order = await _orders.GetAll()
                .Include(o => o.CourierAssignment)
                .FirstOrDefaultAsync(o => o.Id == payment.OrderId);

            if (order is not null)
            {
                if (order.Status == OrderStatus.Pending)
                    order.Status = OrderStatus.Paid;

                await _uow.SaveChangesAsync();

                if (order.CourierAssignment is null)
                    await _couriers.AssignRandomCourierAsync(order.Id);
            }
        }

        
        var userIdStr = session.Metadata?.GetValueOrDefault("userId") ?? session.ClientReferenceId;
        if (Guid.TryParse(userIdStr, out var userId))
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user != null)
            {
                var roleToGrant = session.Metadata?.GetValueOrDefault("grantRole") ?? "StoreSeller";
                if (!string.IsNullOrWhiteSpace(roleToGrant) && !await _userManager.IsInRoleAsync(user, roleToGrant))
                    await _userManager.AddToRoleAsync(user, roleToGrant);
            }
        }

       
        try
        {
            var nowUtc = DateTime.UtcNow;
            DateTime? start = null, end = null;

            if (!string.IsNullOrWhiteSpace(session.SubscriptionId))
            {
                var invService = new InvoiceService();
                var list = await invService.ListAsync(new InvoiceListOptions
                {
                    Subscription = session.SubscriptionId,
                    Status = "paid",
                    Limit = 1
                });

                var inv = list?.Data?.FirstOrDefault();
                var line = inv?.Lines?.Data?.FirstOrDefault();
                var period = line?.Period;

                DateTime? asUtc(DateTime? d) => d.HasValue ? DateTime.SpecifyKind(d.Value, DateTimeKind.Utc) : (DateTime?)null;
                start = asUtc(period?.Start);
                end = asUtc(period?.End);
            }

            var existing = await _db.Set<StoreSellerSubscription>()
                .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);

            if (existing is null)
            {
                existing = new StoreSellerSubscription
                {
                    UserId = userId,
                    Provider = "Stripe",
                    Status = SubscriptionStatus.Active,
                    ExternalCustomerId = session.CustomerId ?? string.Empty,
                    ExternalSubscriptionId = session.SubscriptionId ?? string.Empty,
                    CurrentPeriodStart = start ?? nowUtc,
                    CurrentPeriodEnd = end ?? nowUtc.AddMonths(1),
                    Currency = "AZN",
                    UnitAmountMinor = 0
                };
                await _db.AddAsync(existing);
            }
            else
            {
                existing.Provider = "Stripe";
                existing.Status = SubscriptionStatus.Active;
                if (!string.IsNullOrWhiteSpace(session.CustomerId))
                    existing.ExternalCustomerId = session.CustomerId;
                if (!string.IsNullOrWhiteSpace(session.SubscriptionId))
                    existing.ExternalSubscriptionId = session.SubscriptionId;

                existing.CurrentPeriodStart = start ?? existing.CurrentPeriodStart ?? nowUtc;
                existing.CurrentPeriodEnd = end ?? existing.CurrentPeriodEnd ?? nowUtc.AddMonths(1);

                _db.Update(existing);
            }

            await _db.SaveChangesAsync();
        }
        catch
        {
          
        }
    }

    private async Task HandlePaymentIntentSucceeded(PaymentIntent pi)
    {
        var payment = await _payments.GetAll()
            .FirstOrDefaultAsync(p => p.ProviderPaymentId == pi.Id);

        if (payment != null && payment.Status != PaymentStatus.Succeeded)
        {
            payment.Status = PaymentStatus.Succeeded;
            payment.PaymentDate = DateTime.UtcNow;

            var order = await _orders.GetAll()
                .Include(o => o.CourierAssignment)
                .FirstOrDefaultAsync(o => o.Id == payment.OrderId);

            if (order != null)
            {
                if (order.Status == OrderStatus.Pending)
                    order.Status = OrderStatus.Paid;

                await _uow.SaveChangesAsync();

                if (order.CourierAssignment is null)
                    await _couriers.AssignRandomCourierAsync(order.Id);
            }
        }
    }
}
