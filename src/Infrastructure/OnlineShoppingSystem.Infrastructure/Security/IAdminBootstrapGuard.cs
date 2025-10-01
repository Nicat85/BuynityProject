using System.Net;

namespace OnlineShoppingSystem.Infrastructure.Security;

public interface IAdminBootstrapGuard
{
    Task<bool> IsEnabledAsync();
    Task<bool> IsIpAllowedAsync(IPAddress? remoteIp);
    Task<bool> IsValidTokenAsync(string token);
    Task<bool> ConsumeOnceAsync(); 
}
