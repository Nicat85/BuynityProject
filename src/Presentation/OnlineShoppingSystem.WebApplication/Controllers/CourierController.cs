using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineShppingSystem.Application.Shared.Settings; 
using OnlineSohppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.DTOs.Courier;
using System.Net.Mime;
using static OnlineShppingSystem.Application.Shared.Settings.Permissions;

namespace OnlineShppingSystem.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces(MediaTypeNames.Application.Json)]
public sealed class CourierController : ControllerBase
{
    private readonly ICourierService _couriers;

    public CourierController(ICourierService couriers)
    {
        _couriers = couriers;
    }

   
    [HttpGet("my")]
    [Authorize(Policy = Couriers.ReadMy)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMy(CancellationToken ct = default)
    {
        var resp = await _couriers.GetMyAssignedOrdersAsync(ct);
        return StatusCode((int)resp.StatusCode, resp);
    }

    
    [HttpPost("take/{orderId:guid}")]
    [Authorize(Policy = Couriers.Take)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Take([FromRoute] Guid orderId, CancellationToken ct = default)
    {
        if (orderId == Guid.Empty)
            return BadRequest(new { message = "OrderId boş ola bilməz." });

        var resp = await _couriers.AssignRandomCourierAsync(orderId, ct);
        return StatusCode((int)resp.StatusCode, resp);
    }

   
    [HttpPatch("{orderId:guid}/status")]
    [Authorize(Policy = Couriers.UpdateOrderStatus)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateStatus(
        [FromRoute] Guid orderId,
        [FromBody] CourierStatusUpdateDto dto,
        CancellationToken ct = default)
    {
        if (orderId == Guid.Empty)
            return BadRequest(new { message = "OrderId boş ola bilməz." });

        var resp = await _couriers.UpdateOrderDeliveryStatusAsync(orderId, dto.Status, ct);
        return StatusCode((int)resp.StatusCode, resp);
    }
}
