using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OnlineShppingSystem.Application.Abstracts.Repositories; 
using OnlineShppingSystem.Application.Shared;
using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.DTOs.Payment;
using OnlineSohppingSystem.Application.Shared.Settings;
using OnlineSohppingSystem.Domain.Entities;
using OnlineSohppingSystem.Domain.Enums;
using Stripe;
using System.Net;

namespace OnlineShoppingSystem.Infrastructure.Services;

public sealed class StripePaymentService : IPaymentService
{
    private readonly StripeSettings _stripe;
    private readonly ILogger<StripePaymentService> _logger;
    private readonly IRepository<Payment> _paymentRepo;
    private readonly IRepository<Order> _orderRepo;

    public StripePaymentService(
        IOptions<StripeSettings> stripe,
        ILogger<StripePaymentService> logger,
        IRepository<Payment> paymentRepo,
        IRepository<Order> orderRepo)
    {
        _stripe = stripe.Value;
        _logger = logger;
        _paymentRepo = paymentRepo;
        _orderRepo = orderRepo;

        StripeConfiguration.ApiKey = _stripe.ApiKey;
    }

    public async Task<BaseResponse<CreatePaymentResultDto>> CreatePaymentAsync(CreatePaymentRequestDto request, CancellationToken ct = default)
    {
        try
        {
            
            var orderQuery = _orderRepo.GetAll(true);
            var order = await orderQuery.Include(o => o.Payment)
                                        .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct);

            if (order is null)
                return BaseResponse<CreatePaymentResultDto>.Fail("Order not found.", HttpStatusCode.NotFound);

            if (order.Payment is not null && order.Payment.Status == PaymentStatus.Succeeded)
                return BaseResponse<CreatePaymentResultDto>.Fail("Payment already completed.", HttpStatusCode.BadRequest);

           
            var service = new PaymentIntentService();
            var amountInMinorUnit = (long)(request.Amount * 100m);

            var createOptions = new PaymentIntentCreateOptions
            {
                Amount = amountInMinorUnit,
                Currency = _stripe.Currency.ToLowerInvariant(),
                Description = request.Description ?? $"Order #{order.Id}",
                ReceiptEmail = request.CustomerEmail,
                Metadata = new Dictionary<string, string> { ["order_id"] = order.Id.ToString() },
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },
            };

            var requestOptions = new RequestOptions { IdempotencyKey = $"pay_{order.Id}" };
            var intent = await service.CreateAsync(createOptions, requestOptions, ct);

            
            if (order.Payment is null)
            {
                var payment = new Payment
                {
                    OrderId = order.Id,
                    Amount = request.Amount,
                    PaymentDate = DateTime.UtcNow,
                    PaymentMethod = request.PaymentMethod, 
                    Provider = "Stripe",
                    ProviderPaymentId = intent.Id,
                    Currency = _stripe.Currency.ToUpperInvariant(),
                    Status = MapStatus(intent.Status)
                };

                await _paymentRepo.AddAsync(payment);
            }
            else
            {
                order.Payment.Amount = request.Amount;
                order.Payment.PaymentDate = DateTime.UtcNow;
                order.Payment.PaymentMethod = request.PaymentMethod;
                order.Payment.Provider = "Stripe";
                order.Payment.ProviderPaymentId = intent.Id;
                order.Payment.Currency = _stripe.Currency.ToUpperInvariant();
                order.Payment.Status = MapStatus(intent.Status);

                _paymentRepo.Update(order.Payment);
            }

            await _paymentRepo.SaveChangesAsync(); 

            var result = new CreatePaymentResultDto
            {
                PaymentIntentId = intent.Id,
                ClientSecret = intent.ClientSecret!,
                Status = intent.Status
            };

            return BaseResponse<CreatePaymentResultDto>.CreateSuccess(result, "Payment initialized.", HttpStatusCode.OK);
        }
        catch (StripeException sx)
        {
            _logger.LogError(sx, "Stripe error while creating payment. Code: {Code}, Message: {Message}", sx.StripeError?.Code, sx.Message);
            return BaseResponse<CreatePaymentResultDto>.Fail($"Stripe error: {sx.Message}", HttpStatusCode.BadRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating payment.");
            return BaseResponse<CreatePaymentResultDto>.Fail("Unexpected error.", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<BaseResponse<RefundPaymentResultDto>> RefundPaymentAsync(RefundPaymentRequestDto request, CancellationToken ct = default)
    {
        try
        {
            var payment = await _paymentRepo.GetAll(true)
                                            .FirstOrDefaultAsync(p => p.OrderId == request.OrderId, ct);

            if (payment is null || string.IsNullOrWhiteSpace(payment.ProviderPaymentId))
                return BaseResponse<RefundPaymentResultDto>.Fail("Payment not found.", HttpStatusCode.NotFound);

            if (!string.Equals(payment.Provider, "Stripe", StringComparison.OrdinalIgnoreCase))
                return BaseResponse<RefundPaymentResultDto>.Fail("Unsupported provider.", HttpStatusCode.BadRequest);

            var refundService = new RefundService();
            var options = new RefundCreateOptions
            {
                PaymentIntent = payment.ProviderPaymentId,
                Amount = request.Amount.HasValue ? (long?)(request.Amount.Value * 100m) : null,
                Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : "requested_by_customer"
            };

            var refund = await refundService.CreateAsync(options, cancellationToken: ct);

           
            _paymentRepo.Update(payment);
            await _paymentRepo.SaveChangesAsync();

            var result = new RefundPaymentResultDto
            {
                RefundId = refund.Id,
                Status = refund.Status
            };

            return BaseResponse<RefundPaymentResultDto>.CreateSuccess(result, "Refund created.", HttpStatusCode.OK);
        }
        catch (StripeException sx)
        {
            _logger.LogError(sx, "Stripe error while refunding. Code: {Code}, Message: {Message}", sx.StripeError?.Code, sx.Message);
            return BaseResponse<RefundPaymentResultDto>.Fail($"Stripe error: {sx.Message}", HttpStatusCode.BadRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while refunding payment.");
            return BaseResponse<RefundPaymentResultDto>.Fail("Unexpected error.", HttpStatusCode.InternalServerError);
        }
    }

    private static PaymentStatus MapStatus(string stripeStatus) => stripeStatus switch
    {
        "succeeded" => PaymentStatus.Succeeded,
        "requires_action" or "requires_payment_method" => PaymentStatus.RequiresAction,
        "canceled" => PaymentStatus.Canceled,
        "processing" or "requires_confirmation" => PaymentStatus.Pending,
        _ => PaymentStatus.Failed
    };
}
