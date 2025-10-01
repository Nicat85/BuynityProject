using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Net;

namespace OnlineShoppingSystem.Infrastructure.Security;

public sealed class AdminBootstrapGuard : IAdminBootstrapGuard
{
   
    private const string UsesKey = "admin-bootstrap:uses";

    private readonly IDistributedCache _cache;
    private readonly AdminBootstrapOptions _opt;
    private readonly IConfiguration _config;

    public AdminBootstrapGuard(
        IDistributedCache cache,
        IOptions<AdminBootstrapOptions> opt,
        IConfiguration config)
    {
        _cache = cache;
        _opt = opt.Value ?? new AdminBootstrapOptions();

        _opt.AllowedIPs = (_opt.AllowedIPs ?? new List<string>())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        
        if (_opt.MaxUses < 1) _opt.MaxUses = 1;

        _config = config;
    }

    public async Task<bool> IsEnabledAsync()
    {
        if (!_opt.Enabled) return false;

        var used = await GetUsesAsync();
        return used < _opt.MaxUses; 
    }

    public Task<bool> IsIpAllowedAsync(IPAddress? remoteIp)
    {
        
        if (_opt.AllowedIPs == null || _opt.AllowedIPs.Count == 0 || _opt.AllowedIPs.Contains("*"))
            return Task.FromResult(true);

        if (remoteIp is null)
            return Task.FromResult(false);

        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            remoteIp.ToString()
        };

       
        if (remoteIp.IsIPv4MappedToIPv6)
            candidates.Add(remoteIp.MapToIPv4().ToString());

       
        if (IPAddress.IsLoopback(remoteIp))
        {
            candidates.Add("127.0.0.1");
            candidates.Add("::1");
            candidates.Add("::ffff:127.0.0.1");
        }

        var allowed = candidates.Any(c => _opt.AllowedIPs.Contains(c, StringComparer.OrdinalIgnoreCase));
        return Task.FromResult(allowed);
    }

    public Task<bool> IsValidTokenAsync(string token)
    {
        
        var expected = _config["ADMIN_BOOTSTRAP_TOKEN"];
        if (string.IsNullOrWhiteSpace(expected))
            expected = _config["IdentitySeed:BootstrapToken"];
        if (string.IsNullOrWhiteSpace(expected))
            expected = _config["AdminBootstrap:Token"];

        if (string.IsNullOrWhiteSpace(expected))
            return Task.FromResult(false);

        token = (token ?? string.Empty).Trim();
        expected = expected.Trim();

        return Task.FromResult(ConstantTimeEquals(expected, token));
    }

    public async Task<bool> ConsumeOnceAsync()
    {
        
        var usedAfter = await IncrementUsesAsync();
        return usedAfter <= _opt.MaxUses;
    }

   

    private async Task<int> GetUsesAsync()
    {
        var s = await _cache.GetStringAsync(UsesKey);
        return int.TryParse(s, out var n) ? n : 0;
    }

    private async Task<int> IncrementUsesAsync()
    {
        
        for (int i = 0; i < 3; i++) 
        {
            var current = await GetUsesAsync();
            var next = current + 1;

            await _cache.SetStringAsync(UsesKey, next.ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(3650)
            });

           
            var confirm = await GetUsesAsync();
            if (confirm == next) return next;
        }

       
        var fallback = await GetUsesAsync() + 1;
        await _cache.SetStringAsync(UsesKey, fallback.ToString());
        return fallback;
    }

    private static bool ConstantTimeEquals(string a, string b)
    {
        if (a is null || b is null || a.Length != b.Length) return false;
        var diff = 0;
        for (int i = 0; i < a.Length; i++)
            diff |= a[i] ^ b[i];
        return diff == 0;
    }
}
