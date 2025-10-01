namespace OnlineSohppingSystem.Application.Abstracts.Services;

public interface IAccessTokenService
{
    Task BlacklistAsync(string accessToken, TimeSpan expiry);
    Task<bool> IsBlacklistedAsync(string accessToken);
}

