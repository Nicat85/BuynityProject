using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineShppingSystem.Application.Shared.Helpers;
using OnlineShppingSystem.Application.Shared.Settings;
using OnlineSohppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.DTOs.Payment;

namespace OnlineShppingSystem.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

  
    [HttpPost("create")]
    [Authorize(Policy = Permissions.Payments.Create)]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequestDto dto, CancellationToken ct)
    {
        var resp = await _paymentService.CreatePaymentAsync(dto, ct);
        return StatusCode((int)resp.StatusCode, resp);
    }

    
    [HttpPost("refund")]
    [Authorize(Policy = Permissions.Payments.Refund)]
    public async Task<IActionResult> Refund([FromBody] RefundPaymentRequestDto dto, CancellationToken ct)
    {
        var resp = await _paymentService.RefundPaymentAsync(dto, ct);
        return StatusCode((int)resp.StatusCode, resp);
    }
}
