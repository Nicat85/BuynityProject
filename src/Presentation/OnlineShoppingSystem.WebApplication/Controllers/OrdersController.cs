using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineShppingSystem.Application.Shared.Settings;
using OnlineSohppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.DTOs.Order;

namespace OnlineShppingSystem.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces(MediaTypeNames.Application.Json)]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;
    private readonly ICheckoutService _checkout;

    public OrdersController(IOrderService orders, ICheckoutService checkout)
    {
        _orders = orders;
        _checkout = checkout;
    }

    [HttpPost]
    [Consumes(MediaTypeNames.Application.Json)]
    [Authorize(Policy = Permissions.Orders.Create)]
    public async Task<IActionResult> Create([FromBody] OrderCreateDto dto, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var resp = await _orders.CreateOrderAsync(dto, ct);
        return StatusCode((int)resp.StatusCode, resp);
    }

    [HttpGet("my")]
    [Authorize(Policy = Permissions.Orders.ReadMy)]
    public async Task<IActionResult> GetMy(CancellationToken ct = default)
    {
        var resp = await _orders.GetMyOrdersAsync(ct);
        return StatusCode((int)resp.StatusCode, resp);
    }

   
    [HttpGet("{orderId:guid}/tracking")]
    [Authorize(Policy = Permissions.Orders.ReadById)]
    public async Task<IActionResult> GetTracking([FromRoute] Guid orderId, CancellationToken ct = default)
    {
        var resp = await _orders.GetTrackingAsync(orderId, ct);
        return StatusCode((int)resp.StatusCode, resp);
    }

    [HttpPost("{orderId:guid}/checkout")]
    [Authorize]
    public async Task<IActionResult> CreateCheckout([FromRoute] Guid orderId, CancellationToken ct = default)
    {
        if (orderId == Guid.Empty)
            return BadRequest(new { message = "orderId boş ola bilməz." });

        var resp = await _checkout.CreateOrderCheckoutAsync(orderId, ct);
        if (!resp.IsSuccess)
            return StatusCode((int)resp.StatusCode, resp);

        return Ok(new { url = resp.Data });
    }

    
    [HttpGet("checkout/success")]
    [AllowAnonymous]
    public IActionResult CheckoutSuccess(
        [FromQuery] Guid orderId,
        [FromQuery(Name = "session_id")] string sessionId)
    {
        if (orderId == Guid.Empty || string.IsNullOrWhiteSpace(sessionId))
            return BadRequest(new { message = "Invalid parameters." });

        return Ok(new { message = "Payment verification in progress via webhook", orderId, sessionId });
    }

    [HttpGet("checkout/cancel")]
    [AllowAnonymous]
    public IActionResult CheckoutCancel([FromQuery] Guid orderId)
        => Ok(new { message = "Order payment cancelled", orderId });
}
