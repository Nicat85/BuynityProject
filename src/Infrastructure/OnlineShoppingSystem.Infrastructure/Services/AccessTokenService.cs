using Microsoft.Extensions.Caching.Distributed;
using OnlineSohppingSystem.Application.Abstracts.Services;
using System.Security.Cryptography;
using System.Text;

namespace OnlineShoppingSystem.Infrastructure.Services
{
    public class AccessTokenService : IAccessTokenService
    {
        private readonly IDistributedCache _cache;

        public AccessTokenService(IDistributedCache cache)
        {
            _cache = cache;
        }

        private static string ToBase64Url(byte[] bytes) =>
            Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static string GetKey(string accessToken)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(accessToken));
            return $"blacklist:{ToBase64Url(hash)}";
        }

        public async Task BlacklistAsync(string accessToken, TimeSpan expiry)
        {
            if (expiry <= TimeSpan.Zero) expiry = TimeSpan.FromSeconds(1);

            await _cache.SetStringAsync(
                GetKey(accessToken),
                "1",
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiry
                });
        }

        public async Task<bool> IsBlacklistedAsync(string accessToken)
        {
            var val = await _cache.GetStringAsync(GetKey(accessToken));
            return val is not null;
        }
    }
}
