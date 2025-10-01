using OnlineSohppingSystem.Application.Abstracts.Services;

namespace OnlineShoppingSystem.WebApplication.Middleware;

public class TokenBlacklistMiddleware
{
    private readonly RequestDelegate _next;

    public TokenBlacklistMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, IAccessTokenService accessTokenService)
    {
        
        if (context.Request.Path.StartsWithSegments("/api/payment/webhook", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

        if (!string.IsNullOrEmpty(token))
        {
            var isBlacklisted = await accessTokenService.IsBlacklistedAsync(token);
            if (isBlacklisted)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Access token is blacklisted");
                return;
            }
        }

        await _next(context);
    }
}
