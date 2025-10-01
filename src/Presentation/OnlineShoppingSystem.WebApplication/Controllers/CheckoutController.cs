using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OnlineShppingSystem.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CheckoutController : ControllerBase
{
    [HttpGet("success")]
    [AllowAnonymous]
    public IActionResult Success([FromQuery] Guid orderId)
    {
        
        var html = $"""
        <html><body style="font-family:sans-serif">
        <h2>Ödəniş tamamlandı ✅</h2>
        <p>Sifariş: <b>{orderId}</b></p>
        <p>İndi tətbiqinizdə “My Orders” səhifəsinə qayıda bilərsiniz.</p>
        </body></html>
        """;
        return new ContentResult { Content = html, ContentType = "text/html" };
    }

    [HttpGet("cancel")]
    [AllowAnonymous]
    public IActionResult Cancel([FromQuery] Guid orderId)
    {
        var html = $"""
        <html><body style="font-family:sans-serif">
        <h2>Ödəniş ləğv olundu ❌</h2>
        <p>Sifariş: <b>{orderId}</b></p>
        <p>İstəsəniz yenidən Checkout yarada bilərsiniz.</p>
        </body></html>
        """;
        return new ContentResult { Content = html, ContentType = "text/html" };
    }
}
