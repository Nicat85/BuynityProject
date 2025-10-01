using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineShppingSystem.Application.Shared;
using OnlineSohppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.DTOs.Subscription;

[ApiController]
[Route("api/profile/subscription")]
public class ProfileSubscriptionController : ControllerBase
{
    private readonly IStoreSellerSubscriptionService _svc;

    public ProfileSubscriptionController(IStoreSellerSubscriptionService svc) => _svc = svc;

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("start")]
    [Authorize]
    public async Task<IActionResult> Start([FromBody] StartStoreSellerSubscriptionRequest req)
    {
        var userId = CurrentUserId;
        var planCode = string.IsNullOrWhiteSpace(req.PlanCode) ? "store_seller_monthly" : req.PlanCode;

       
        var statusResp = await _svc.GetMyStatusAsync(userId, HttpContext.RequestAborted);
        if (statusResp.IsSuccess && statusResp.Data?.IsActive == true)
        {
            var conflict = BaseResponse<string>.Fail("Plan artıq aktivdir. Ödəniş linki yaradılmadı.", HttpStatusCode.Conflict);
            return StatusCode((int)conflict.StatusCode, conflict);
        }

        
        if (!string.IsNullOrWhiteSpace(req.SuccessUrl))
        {
            var uri = new Uri(req.SuccessUrl);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            foreach (var key in query.AllKeys.Where(k => k != null))
                if (query[key] != null) query[key] = Uri.EscapeDataString(query[key]!);
            var builder = new UriBuilder(uri) { Query = query.ToString() };
            req.SuccessUrl = builder.Uri.ToString();
        }
        if (!string.IsNullOrWhiteSpace(req.CancelUrl))
        {
            var uri = new Uri(req.CancelUrl);
            var builder = new UriBuilder(uri);
            req.CancelUrl = builder.Uri.ToString();
        }

        var resp = await _svc.StartAsync(userId, req, HttpContext.RequestAborted);
        if (!resp.IsSuccess || resp.Data is null || string.IsNullOrWhiteSpace(resp.Data.CheckoutUrl))
            return StatusCode((int)resp.StatusCode, resp);

        return StatusCode((int)resp.StatusCode, new
        {
            CheckoutUrl = resp.Data.CheckoutUrl,
            resp.Message,
            resp.IsSuccess,
            resp.StatusCode
        });
    }
}
