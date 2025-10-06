using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineShoppingSystem.Persistence.Contexts;
using OnlineShppingSystem.Application.Abstracts.Repositories;
using OnlineShppingSystem.Application.Shared;
using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Application.Abstracts.Repositories;
using OnlineSohppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.DTOs.Order;
using OnlineSohppingSystem.Domain.Entities;
using OnlineSohppingSystem.Domain.Enums;
using System.Net;
using System.Security.Claims;

using OrderItemEntity = OnlineSohppingSystem.Domain.Entities.OrderItem;
using PaymentEntity = OnlineSohppingSystem.Domain.Entities.Payment;

namespace OnlineShoppingSystem.Persistence.Services;

public sealed class OrderService : IOrderService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IHttpContextAccessor _http;
    private readonly IRepository<Order> _orderRepo;
    private readonly IOrderItemRepository _orderItemRepo;
    private readonly IRepository<Product> _productRepo;
    private readonly IRepository<PaymentEntity> _paymentRepo;
    private readonly IUnitOfWork _uow;
    private readonly OnlineShoppingSystemDbContext _ctx;
    private readonly ILogger<OrderService> _log;
    private readonly IMapper _mapper;

    public OrderService(
        UserManager<AppUser> userManager,
        IHttpContextAccessor http,
        IRepository<Order> orderRepo,
        IOrderItemRepository orderItemRepo,
        IRepository<Product> productRepo,
        IRepository<PaymentEntity> paymentRepo,
        IUnitOfWork uow,
        OnlineShoppingSystemDbContext ctx,
        ILogger<OrderService> log,
        IMapper mapper) 
    {
        _userManager = userManager;
        _http = http;
        _orderRepo = orderRepo;
        _orderItemRepo = orderItemRepo;
        _productRepo = productRepo;
        _paymentRepo = paymentRepo;
        _uow = uow;
        _ctx = ctx;
        _log = log;
        _mapper = mapper;
    }

    public async Task<BaseResponse<OrderResultDto>> CreateOrderAsync(OrderCreateDto request, CancellationToken ct = default)
    {
        var buyerId = ResolveUserId();
        if (buyerId == Guid.Empty)
            return BaseResponse<OrderResultDto>.Fail("Unauthorized", HttpStatusCode.Unauthorized);

        var buyer = await _userManager.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => !u.IsDeleted && u.Id == buyerId, ct);

        if (buyer is null)
            return BaseResponse<OrderResultDto>.Fail("Buyer not found", HttpStatusCode.BadRequest);

        if (request?.Items is null || request.Items.Count == 0)
            return BaseResponse<OrderResultDto>.Fail("Məhsul siyahısı boş ola bilməz.", HttpStatusCode.BadRequest);

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _productRepo
            .GetAll(isTracking: true)
            .Where(p => !p.IsDeleted && productIds.Contains(p.Id))
            .ToListAsync(ct);

        if (products.Count != productIds.Count)
            return BaseResponse<OrderResultDto>.Fail("Bəzi məhsullar tapılmadı və ya silinib.", HttpStatusCode.BadRequest);

        var paymentMethod = request.PaymentMethod;

        decimal total = 0m;
        var now = DateTime.UtcNow;
        var orderItemBuffer = new List<OrderItemEntity>();

        foreach (var item in request.Items)
        {
            var product = products.First(p => p.Id == item.ProductId);

            if (product.UserId == buyerId)
                return BaseResponse<OrderResultDto>.Fail("Öz məhsulunuzu ala bilməzsiniz.", HttpStatusCode.BadRequest);

            if (product.Status == ProductStatus.Inactive)
                return BaseResponse<OrderResultDto>.Fail($"Məhsul satışda deyil: {product.Name}", HttpStatusCode.BadRequest);

            if (item.Quantity <= 0)
                return BaseResponse<OrderResultDto>.Fail($"Miqdar 0-dan böyük olmalıdır: {product.Name}", HttpStatusCode.BadRequest);

            if (product.StockQuantity < item.Quantity)
                return BaseResponse<OrderResultDto>.Fail(
                    $"Kifayət qədər stok yoxdur: {product.Name}. Mövcud: {product.StockQuantity}",
                    HttpStatusCode.BadRequest);

            total += product.Price * item.Quantity;

            orderItemBuffer.Add(new OrderItemEntity
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.Price,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false
            });
        }

        _log.LogInformation("CreateOrder: DB={Db}, BuyerId={BuyerId}, Email={Email}",
            _ctx.Database.GetDbConnection().Database, buyer.Id, buyer.Email);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            BuyerId = buyer.Id,
            OrderDate = now,
            TotalPrice = total,
            Status = OrderStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };
        await _orderRepo.AddAsync(order);

        foreach (var oi in orderItemBuffer)
        {
            oi.OrderId = order.Id;
            await _orderItemRepo.AddAsync(oi);

            var product = products.First(p => p.Id == oi.ProductId);
            product.StockQuantity -= oi.Quantity;
            if (product.StockQuantity < 0) product.StockQuantity = 0;
            _productRepo.Update(product);
        }

        var payment = new PaymentEntity
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Amount = total,
            Currency = "azn", 
            PaymentMethod = paymentMethod,
            Provider = null,
            ProviderPaymentId = null,
            PaymentDate = now,
            Status = PaymentStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };
        await _paymentRepo.AddAsync(payment);

        await _uow.SaveChangesAsync();

        var result = new OrderResultDto
        {
            OrderId = order.Id,
            OrderDate = order.OrderDate,
            TotalPrice = order.TotalPrice,
            Status = order.Status,
            PaymentMethod = payment.PaymentMethod.ToString(),
            Items = orderItemBuffer.Select(oi =>
            {
                var p = products.First(pp => pp.Id == oi.ProductId);
                return new OrderItemResultDto
                {
                    ProductId = oi.ProductId,
                    ProductName = p.Name,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                };
            }).ToList()
        };

        return BaseResponse<OrderResultDto>.CreateSuccess(result, "Sifariş yaradıldı.", HttpStatusCode.OK);
    }

    public async Task<BaseResponse<OrderResultDto>> GetByIdAsync(Guid orderId, CancellationToken ct = default)
    {
        var buyerId = ResolveUserId();
        if (buyerId == Guid.Empty)
            return BaseResponse<OrderResultDto>.Fail("Unauthorized", HttpStatusCode.Unauthorized);

        var order = await _orderRepo
            .GetAll(isTracking: false)
            .Where(o => !o.IsDeleted && o.Id == orderId && o.BuyerId == buyerId)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(ct);

        if (order is null)
            return BaseResponse<OrderResultDto>.Fail("Sifariş tapılmadı.", HttpStatusCode.NotFound);

        var dto = new OrderResultDto
        {
            OrderId = order.Id,
            OrderDate = order.OrderDate,
            TotalPrice = order.TotalPrice,
            Status = order.Status,
            PaymentMethod = order.Payment?.PaymentMethod.ToString() ?? string.Empty,
            Items = order.OrderItems.Select(oi => new OrderItemResultDto
            {
                ProductId = oi.ProductId,
                ProductName = oi.Product?.Name ?? string.Empty,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice
            }).ToList()
        };

        return BaseResponse<OrderResultDto>.CreateSuccess(dto);
    }

    public async Task<BaseResponse<List<OrderResultDto>>> GetMyOrdersAsync(CancellationToken ct = default)
    {
        var buyerId = ResolveUserId();
        if (buyerId == Guid.Empty)
            return BaseResponse<List<OrderResultDto>>.Fail("Unauthorized", HttpStatusCode.Unauthorized);

        var orders = await _orderRepo
            .GetAll(isTracking: false)
            .Where(o => !o.IsDeleted && o.BuyerId == buyerId)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Include(o => o.Payment)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(ct);

        var list = orders.Select(o => new OrderResultDto
        {
            OrderId = o.Id,
            OrderDate = o.OrderDate,
            TotalPrice = o.TotalPrice,
            Status = o.Status,
            PaymentMethod = o.Payment?.PaymentMethod.ToString() ?? string.Empty,
            Items = o.OrderItems.Select(oi => new OrderItemResultDto
            {
                ProductId = oi.ProductId,
                ProductName = oi.Product?.Name ?? string.Empty,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice
            }).ToList()
        }).ToList();

        return BaseResponse<List<OrderResultDto>>.CreateSuccess(list);
    }

    public async Task<BaseResponse<bool>> MarkAsPaidAsync(Guid orderId, string providerPaymentId, CancellationToken ct = default)
    {
        
        var order = await _orderRepo
            .GetAll(isTracking: true)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
            return BaseResponse<bool>.Fail("Sifariş tapılmadı.", HttpStatusCode.NotFound);

        if (order.Status == OrderStatus.Paid || order.Payment?.Status == PaymentStatus.Succeeded)
            return BaseResponse<bool>.CreateSuccess(true, "Sifariş artıq ödənilib.");

        order.Status = OrderStatus.Paid;

        if (order.Payment is null)
        {
            
            var newPayment = new PaymentEntity
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Provider = "Stripe",
                ProviderPaymentId = providerPaymentId,
                PaymentMethod = PaymentMethod.Card,
                Status = PaymentStatus.Succeeded,
                Amount = order.TotalPrice,
                Currency = "azn",
                PaymentDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            await _paymentRepo.AddAsync(newPayment);
            order.Payment = newPayment;
        }
        else
        {
            order.Payment.Provider = "Stripe";
            order.Payment.ProviderPaymentId = providerPaymentId;
            order.Payment.Status = PaymentStatus.Succeeded;
            order.Payment.PaymentDate = DateTime.UtcNow;
            order.Payment.Amount = order.TotalPrice;
            order.Payment.UpdatedAt = DateTime.UtcNow;
        }

        await _uow.SaveChangesAsync();
       
        return BaseResponse<bool>.CreateSuccess(true, "Ödəniş tamamlandı.");
    }
    public async Task<BaseResponse<OrderTrackingDto>> GetTrackingAsync(Guid orderId, CancellationToken ct = default)
    {
        var buyerId = ResolveUserId();
        if (buyerId == Guid.Empty)
            return BaseResponse<OrderTrackingDto>.Fail("Unauthorized", HttpStatusCode.Unauthorized);

        var order = await _orderRepo.GetAll(isTracking: false)
            .Include(o => o.Payment)
            .Include(o => o.CourierAssignment).ThenInclude(ca => ca.Courier)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.BuyerId == buyerId, ct);

        if (order is null)
            return BaseResponse<OrderTrackingDto>.Fail("Sifariş tapılmadı.", HttpStatusCode.NotFound);

        var dto = new OrderTrackingDto
        {
            OrderId = order.Id,
            Status = order.Status.ToString(),
            StatusCode = (int)order.Status,
            PlacedAt = order.OrderDate,
            PaidAt = order.Payment?.Status == PaymentStatus.Succeeded ? order.Payment?.PaymentDate : null,
            ShippedAt = order.CourierAssignment?.PickedUpAt,
            DeliveredAt = order.CourierAssignment?.DeliveredAt,
            CourierName = order.CourierAssignment?.Courier?.FullName
        };

        return BaseResponse<OrderTrackingDto>.CreateSuccess(dto);
    }

    public async Task<BaseResponse<List<OrderResultDto>>> GetAllOrdersAsync(CancellationToken ct)
    {
        var dto = await _orderRepo
            .GetAll()
            .AsNoTracking()
            .Include(o => o.Buyer)
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
            .Include(o => o.Payment)
            .Select(o => new OrderResultDto
            {
                OrderId = o.Id,
                BuyerName = o.Buyer != null ? o.Buyer.UserName : null,
                BuyerPhone = o.Buyer != null ? o.Buyer.PhoneNumber : null,
                BuyerAddress = o.Buyer != null ? o.Buyer.Address : null,
                OrderDate = o.CreatedAt,
                TotalPrice = o.OrderItems.Sum(i => i.UnitPrice * i.Quantity),
                Status = o.Status,

                
                PaymentMethod = o.Payment != null
                    ? o.Payment.PaymentMethod.ToString()
                    : "Unknown",

                Items = o.OrderItems.Select(i => new OrderItemResultDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product != null ? i.Product.Name : null,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            })
            .ToListAsync(ct);

        return new BaseResponse<List<OrderResultDto>>
        {
            Data = dto,
            Message = "Bütün sifarişlər uğurla gətirildi.",
            IsSuccess = true,
            StatusCode = HttpStatusCode.OK
        };
    }





    private Guid ResolveUserId()
    {
        var sid = _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sid, out var id) ? id : Guid.Empty;
    }
}
