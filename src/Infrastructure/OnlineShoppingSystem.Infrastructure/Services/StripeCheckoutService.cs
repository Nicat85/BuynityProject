using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnlineShppingSystem.Application.Abstracts.Repositories;
using OnlineShppingSystem.Application.Shared;
using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.Shared.Settings;
using OnlineSohppingSystem.Domain.Entities;
using OnlineSohppingSystem.Domain.Enums;
using Stripe;
using Stripe.Checkout;
using System.Net;
using System.Security.Claims;
using PaymentMethod = OnlineSohppingSystem.Domain.Enums.PaymentMethod;

namespace OnlineShoppingSystem.Persistence.Services
{
    public sealed class StripeCheckoutService : ICheckoutService
    {
        private readonly IRepository<Order> _orders;
        private readonly IRepository<Payment> _payments;
        private readonly IUnitOfWork _uow;
        private readonly StripeSettings _stripe;
        private readonly IHttpContextAccessor _http;

        public StripeCheckoutService(
            IRepository<Order> orders,
            IRepository<Payment> payments,
            IUnitOfWork uow,
            IOptions<StripeSettings> stripe,
            IHttpContextAccessor http)
        {
            _orders = orders;
            _payments = payments;
            _uow = uow;
            _stripe = stripe.Value ?? throw new ArgumentNullException(nameof(stripe));
            _http = http;
            Stripe.StripeConfiguration.ApiKey = _stripe.ApiKey;
        }

        public async Task<BaseResponse<string>> CreateOrderCheckoutAsync(Guid orderId, CancellationToken ct = default)
        {
            try
            {
                var userIdStr = _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdStr, out var userId) || userId == Guid.Empty)
                    return BaseResponse<string>.Fail("Unauthorized", HttpStatusCode.Unauthorized);

                
                var order = await _orders.GetAll(isTracking: true)
                    .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                    .Include(o => o.Payment)
                    .FirstOrDefaultAsync(o => o.Id == orderId, ct);

                if (order is null)
                    return BaseResponse<string>.Fail("Sifariş tapılmadı.", HttpStatusCode.NotFound);

                if (order.BuyerId != userId)
                    return BaseResponse<string>.Fail("Bu sifariş sənə aid deyil.", HttpStatusCode.Forbidden);

                StripeConfiguration.ApiKey = _stripe.ApiKey;
                var currency = (_stripe.Currency ?? "azn").ToLowerInvariant();

                
                if (order.Status == OrderStatus.Paid || order.Payment?.Status == PaymentStatus.Succeeded)
                    return BaseResponse<string>.Fail("Sifariş artıq ödənilib.", HttpStatusCode.Conflict);

               
                if (!string.IsNullOrWhiteSpace(order.Payment?.ProviderPaymentId))
                {
                    try
                    {
                        var ss = new SessionService();
                        var existing = await ss.GetAsync(order.Payment.ProviderPaymentId, cancellationToken: ct);

                        if (existing is not null)
                        {
                            var isPaid = string.Equals(existing.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase)
                                         || string.Equals(existing.Status, "complete", StringComparison.OrdinalIgnoreCase);

                            if (isPaid)
                            {
                                
                                order.Status = OrderStatus.Paid;
                                order.Payment!.Status = PaymentStatus.Succeeded;
                                order.Payment.ProviderPaymentId = existing.Id;
                                order.Payment.PaymentDate = DateTime.UtcNow;
                                await _uow.SaveChangesAsync();

                                

                                return BaseResponse<string>.Fail("Sifariş artıq ödənilib.", HttpStatusCode.Conflict);
                            }

                            
                            if (string.Equals(existing.Status, "open", StringComparison.OrdinalIgnoreCase)
                                && !string.IsNullOrWhiteSpace(existing.Url))
                            {
                                return BaseResponse<string>.CreateSuccess(existing.Url!, "Mövcud checkout linki.");
                            }
                        }
                    }
                    catch
                    {
                        
                    }
                }

                
                if (order.Status != OrderStatus.Pending)
                    return BaseResponse<string>.Fail("Yalnız Pending sifariş üçün checkout yaradıla bilər.", HttpStatusCode.BadRequest);

                if (order.TotalPrice <= 0 || order.OrderItems.Count == 0)
                    return BaseResponse<string>.Fail("Sifariş məbləği və ya məhsullar düzgün deyil.", HttpStatusCode.BadRequest);

                if (order.Payment is null || order.Payment.PaymentMethod != PaymentMethod.Card)
                    return BaseResponse<string>.Fail("Bu sifariş üçün kartla ödəniş aktiv deyil.", HttpStatusCode.BadRequest);

                
                var lineItems = order.OrderItems.Select(oi => new SessionLineItemOptions
                {
                    Quantity = oi.Quantity,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = currency,
                        UnitAmount = (long)Math.Round(oi.UnitPrice * 100m),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = oi.Product?.Name ?? "Product"
                        }
                    }
                }).ToList();

                
                var successUrl = (_stripe.SuccessUrl ?? "https://localhost:7237/api/orders/checkout/success?orderId={ORDER_ID}&session_id={CHECKOUT_SESSION_ID}")
                                 .Replace("{ORDER_ID}", order.Id.ToString());
                var cancelUrl = (_stripe.CancelUrl ?? "https://localhost:7237/api/orders/checkout/cancel?orderId={ORDER_ID}")
                                .Replace("{ORDER_ID}", order.Id.ToString());

                
                var options = new SessionCreateOptions
                {
                    Mode = "payment",
                    SuccessUrl = successUrl,
                    CancelUrl = cancelUrl,
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = lineItems,
                    ClientReferenceId = order.Id.ToString(),
                    Metadata = new Dictionary<string, string>
                    {
                        ["orderId"] = order.Id.ToString(),
                        ["userId"] = order.BuyerId.ToString()
                    }
                };

                var sessionServiceNew = new SessionService();
                var session = await sessionServiceNew.CreateAsync(options, cancellationToken: ct);

                
                if (order.Payment is null)
                {
                    var pay = new Payment
                    {
                        OrderId = order.Id,
                        PaymentMethod = PaymentMethod.Card,
                        Provider = "Stripe",
                        ProviderPaymentId = session.Id,
                        Currency = currency,
                        Amount = order.TotalPrice,
                        Status = PaymentStatus.RequiresAction,
                        PaymentDate = DateTime.UtcNow
                    };
                    await _payments.AddAsync(pay);
                }
                else
                {
                    order.Payment.Provider = "Stripe";
                    order.Payment.ProviderPaymentId = session.Id;
                    order.Payment.Status = PaymentStatus.RequiresAction;
                    order.Payment.Amount = order.TotalPrice;
                    order.Payment.Currency = currency;
                    order.Payment.PaymentDate = DateTime.UtcNow;
                }

                await _uow.SaveChangesAsync();

                return BaseResponse<string>.CreateSuccess(session.Url!, "Checkout link yaradıldı.");
            }
            catch (StripeException se)
            {
                return BaseResponse<string>.Fail($"Stripe error: {se.Message}", HttpStatusCode.BadGateway);
            }
            catch (Exception ex)
            {
                return BaseResponse<string>.Fail($"Server error: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }


    }
}
