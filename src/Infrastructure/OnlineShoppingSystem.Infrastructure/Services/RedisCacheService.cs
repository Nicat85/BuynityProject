using Microsoft.Extensions.Caching.Distributed;
using OnlineShppingSystem.Application.Abstracts.Services;

namespace OnlineShoppingSystem.Infrastructure.Services;

public class RedisCacheService : IRedisCacheService
{
    private readonly IDistributedCache _distributedCache;

    public RedisCacheService(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    public async Task SetAsync(string key, string value, TimeSpan? expiry = null)
    {
        var options = new DistributedCacheEntryOptions();

        if (expiry.HasValue)
            options.AbsoluteExpirationRelativeToNow = expiry.Value;

        await _distributedCache.SetStringAsync(key, value, options);
    }

    public async Task<string?> GetAsync(string key)
    {
        return await _distributedCache.GetStringAsync(key);
    }

    public async Task RemoveAsync(string key)
    {
        await _distributedCache.RemoveAsync(key);
    }
}
