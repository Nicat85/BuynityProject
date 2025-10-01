using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineShppingSystem.Application.Abstracts.Repositories;
using OnlineShppingSystem.Application.Common.Extensions;
using OnlineShppingSystem.Application.Shared;
using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Domain.Enums;
using System.Net;
using OnlineSohppingSystem.Application.DTOs.Courier;

namespace OnlineShoppingSystem.Infrastructure.Services;

public sealed class CourierService : ICourierService
{
    private readonly IRepository<Order> _orders;
    private readonly IRepository<CourierAssignment> _assignments;
    private readonly UserManager<AppUser> _userManager;
    private readonly IHttpContextAccessor _http;
    private readonly IUnitOfWork _uow;

    public CourierService(
        IRepository<Order> orders,
        IRepository<CourierAssignment> assignments,
        UserManager<AppUser> userManager,
        IHttpContextAccessor http,
        IUnitOfWork uow)
    {
        _orders = orders;
        _assignments = assignments;
        _userManager = userManager;
        _http = http;
        _uow = uow;
    }

    public async Task<BaseResponse> AssignRandomCourierAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _orders.GetAll(isTracking: true)
            .Include(o => o.CourierAssignment)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null) return BaseResponse.Fail("Sifariş tapılmadı.", HttpStatusCode.NotFound);
        if (order.CourierAssignment is not null) return BaseResponse.Fail("Sifarişə kuryer təyin edilib.", HttpStatusCode.BadRequest);

        var couriers = await _userManager.GetUsersInRoleAsync("COURIER");
        var available = couriers.Where(c => !c.IsDeleted).ToList();
        if (available.Count == 0) return BaseResponse.Fail("Aktiv kuryer yoxdur.", HttpStatusCode.Conflict);

        var courier = available[Random.Shared.Next(available.Count)];

        var ca = new CourierAssignment
        {
            OrderId = order.Id,
            CourierId = courier.Id,
            Status = CourierAssignmentStatus.Assigned,
            AssignedAt = DateTime.UtcNow
        };
        await _assignments.AddAsync(ca);

        
        await _uow.SaveChangesAsync();

        return BaseResponse.Success("Kuryer təyin edildi.");
    }

    public async Task<BaseResponse> UpdateOrderDeliveryStatusAsync(
        Guid orderId,
        CourierAssignmentStatus status,
        CancellationToken ct = default)
    {
        var userId = _http.HttpContext?.User.GetUserId() ?? Guid.Empty;
        if (userId == Guid.Empty) return BaseResponse.Fail("Unauthorized", HttpStatusCode.Unauthorized);

       
        var assignment = await _assignments.GetAll(isTracking: true)
            .Include(a => a.Order)
            .FirstOrDefaultAsync(a => a.OrderId == orderId, ct);

        if (assignment is null) return BaseResponse.Fail("Təyinat tapılmadı.", HttpStatusCode.NotFound);
        if (assignment.CourierId != userId) return BaseResponse.Fail("Bu sifariş sizin deyil.", HttpStatusCode.Forbidden);

        assignment.Status = status;

       
        if (status == CourierAssignmentStatus.PickedUp)
        {
            assignment.PickedUpAt = DateTime.UtcNow;
            assignment.Order.Status = OrderStatus.Shipped;
        }
        else if (status == CourierAssignmentStatus.Delivered)
        {
            assignment.DeliveredAt = DateTime.UtcNow;
            assignment.Order.Status = OrderStatus.Delivered;
        }
        else if (status == CourierAssignmentStatus.Canceled)
        {
            assignment.Order.Status = OrderStatus.Canceled;
        }

        await _uow.SaveChangesAsync();
        return BaseResponse.Success("Status yeniləndi.");
    }

    public async Task<BaseResponse<List<CourierOrderDto>>> GetMyAssignedOrdersAsync(CancellationToken ct = default)
    {
        var userId = _http.HttpContext?.User.GetUserId() ?? Guid.Empty;
        if (userId == Guid.Empty) return BaseResponse<List<CourierOrderDto>>.Fail("Unauthorized", HttpStatusCode.Unauthorized);

        var list = await _assignments.GetAll(isTracking: false)
            .Where(a => a.CourierId == userId)
            .Include(a => a.Order).ThenInclude(o => o.Buyer)
            .Include(a => a.Order).ThenInclude(o => o.OrderItems).ThenInclude(oi => oi.Product).ThenInclude(p => p.User)
            .OrderByDescending(a => a.AssignedAt)
            .Select(a => new CourierOrderDto
            {
                OrderId = a.OrderId,
                OrderStatus = a.Order.Status,
                TotalPrice = a.Order.TotalPrice,
                BuyerFullName = a.Order.Buyer.FullName ?? string.Empty,
                BuyerAddress = a.Order.Buyer.Address ?? string.Empty,
                SellerName = a.Order.OrderItems.Select(z => z.Product.User.FullName).FirstOrDefault() ?? string.Empty,
                SellerAddress = a.Order.OrderItems.Select(z => z.Product.User.Address ?? string.Empty).FirstOrDefault() ?? string.Empty,
                AssignedAt = a.AssignedAt
            })
            .ToListAsync(ct);

        return BaseResponse<List<CourierOrderDto>>.CreateSuccess(list);
    }
}
