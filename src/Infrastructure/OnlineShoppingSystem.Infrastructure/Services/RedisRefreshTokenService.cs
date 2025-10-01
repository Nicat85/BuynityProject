using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace OnlineShoppingSystem.Infrastructure.Services
{
    public class RedisRefreshTokenService
    {
        private readonly IDistributedCache _cache;
        private readonly TimeSpan _tokenExpiry;

        public RedisRefreshTokenService(IDistributedCache cache, IConfiguration configuration)
        {
            _cache = cache;

            
            if (int.TryParse(configuration["JWTSettings:RefreshTokenExpirationDays"], out var days) && days > 0)
                _tokenExpiry = TimeSpan.FromDays(days);
            else if (int.TryParse(configuration["JWTSettings:RefreshTokenExpirationMinutes"], out var mins) && mins > 0)
                _tokenExpiry = TimeSpan.FromMinutes(mins);
            else
                _tokenExpiry = TimeSpan.FromDays(7);
        }

        private static string ToBase64Url(byte[] bytes) =>
            Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static string GetKey(string token)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
            return $"refresh_token:{ToBase64Url(hash)}";
        }

        public async Task StoreAsync(string token, Guid userId)
        {
            await _cache.SetStringAsync(
                GetKey(token),
                userId.ToString(),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _tokenExpiry
                });
        }

        public async Task<Guid?> ValidateAsync(string token)
        {
            var value = await _cache.GetStringAsync(GetKey(token));
            return Guid.TryParse(value, out var userId) ? userId : null;
        }

        public async Task RemoveAsync(string token)
        {
            await _cache.RemoveAsync(GetKey(token));
        }
    }
}
