using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OnlineShppingSystem.Application.Abstracts.Services;
using OnlineShppingSystem.Application.Authorization;
using OnlineShppingSystem.Application.DTOs.TokenDto;
using OnlineShppingSystem.Application.Shared;
using OnlineShppingSystem.Application.Shared.Helpers;
using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Application.Events;

namespace OnlineShoppingSystem.Infrastructure.Services
{
    public class FacebookService : IFacebookService
    {
        private readonly HttpClient _httpClient;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly ITokenService _tokenService;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IConfiguration _configuration;

        public FacebookService(
            HttpClient httpClient,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            ITokenService tokenService,
            IPublishEndpoint publishEndpoint,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _publishEndpoint = publishEndpoint;
            _configuration = configuration;
        }

        public async Task<BaseResponse<TokenDto>> LoginWithFacebookAsync(string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return BaseResponse<TokenDto>.Fail("Access token is required.", HttpStatusCode.BadRequest);

            var appSecret = _configuration["FacebookAuthSettings:AppSecret"];
            var appSecretProof = !string.IsNullOrWhiteSpace(appSecret)
                ? CreateAppSecretProof(accessToken, appSecret!)
                : null;

            JsonElement root; 

            try
            {
                var url = new StringBuilder("https://graph.facebook.com/v19.0/me")
                    .Append("?fields=id,email,name,picture.type(large){url}")
                    .Append("&access_token=").Append(Uri.EscapeDataString(accessToken));

                if (!string.IsNullOrWhiteSpace(appSecretProof))
                    url.Append("&appsecret_proof=").Append(appSecretProof);

                var fbResponse = await _httpClient.GetAsync(url.ToString());
                if (!fbResponse.IsSuccessStatusCode)
                    return BaseResponse<TokenDto>.Fail("Facebook login failed.", HttpStatusCode.BadRequest);

                
                await using var stream = await fbResponse.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(stream);
                root = doc.RootElement.Clone(); 
            }
            catch (Exception ex)
            {
                return BaseResponse<TokenDto>.Fail($"Facebook login failed: {ex.Message}", HttpStatusCode.BadRequest);
            }

            var fbId = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
            var email = root.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
            var name = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;

            string? pictureUrl = null;
            if (root.TryGetProperty("picture", out var pic) && pic.ValueKind == JsonValueKind.Object &&
                pic.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object &&
                data.TryGetProperty("url", out var urlProp))
            {
                pictureUrl = urlProp.GetString();
            }

            if (string.IsNullOrWhiteSpace(fbId))
                return BaseResponse<TokenDto>.Fail("Facebook id not found.", HttpStatusCode.BadRequest);

            if (string.IsNullOrWhiteSpace(email))
                return BaseResponse<TokenDto>.Fail("Email permission is required from Facebook profile.", HttpStatusCode.BadRequest);

            var provider = "Facebook";
            var providerKey = fbId;
            var nowUtc = DateTime.UtcNow;

            var linkedUser = await _userManager.FindByLoginAsync(provider, providerKey);
            AppUser? user = linkedUser;

            if (user == null)
            {
                var normalizedEmail = email.Trim().ToLowerInvariant();
                user = await _userManager.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == normalizedEmail);
            }

            if (user != null)
            {
                if (user.IsDeleted && user.DeletedAt.HasValue && (nowUtc - user.DeletedAt.Value).TotalDays <= 30)
                {
                    user.IsDeleted = false;
                    user.DeletedAt = null;
                    await _userManager.UpdateAsync(user);
                }
                else if (user.IsDeleted || (user.LastLoginDate.HasValue && (nowUtc - user.LastLoginDate.Value).TotalDays >= 365))
                {
                    await _userManager.DeleteAsync(user);
                    user = null;
                }
            }

            bool isNewUser = false;

            if (user == null)
            {
                var userName = BuildUniqueUsernameFromEmailLocalPart(await _userManager.Users.ToListAsync(), email!);
                var displayName = string.IsNullOrWhiteSpace(name) ? userName : name!.Trim();
                var avatarText = AvatarHelper.GenerateAvatarText(displayName);

                user = new AppUser
                {
                    Email = email!.Trim(),
                    UserName = userName,
                    FullName = displayName,
                    EmailConfirmed = true,
                    LastLoginDate = nowUtc,
                    IsDeleted = false,
                    AvatarText = avatarText,
                    ProfilePicture = !string.IsNullOrWhiteSpace(pictureUrl)
                        ? pictureUrl
                        : $"https://ui-avatars.com/api/?name={avatarText}&background=random&size=360"
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    return new BaseResponse<TokenDto>("User creation failed.", HttpStatusCode.InternalServerError)
                    {
                        Errors = createResult.Errors.Select(e => e.Description).ToList(),
                        IsSuccess = false
                    };
                }

                await AuthorizationSyncHelper.AssignRoleAndSyncUserClaimsAsync(_userManager, _roleManager, user, RoleTemplates.Buyer);

                var addLoginResult = await _userManager.AddLoginAsync(user, new UserLoginInfo(provider, providerKey, provider));
                if (!addLoginResult.Succeeded)
                {
                    return new BaseResponse<TokenDto>("External login binding failed.", HttpStatusCode.InternalServerError)
                    {
                        Errors = addLoginResult.Errors.Select(e => e.Description).ToList(),
                        IsSuccess = false
                    };
                }

                isNewUser = true;
            }
            else
            {
                var logins = await _userManager.GetLoginsAsync(user);
                if (!logins.Any(l => l.LoginProvider == provider && l.ProviderKey == providerKey))
                {
                    var addLoginResult = await _userManager.AddLoginAsync(user, new UserLoginInfo(provider, providerKey, provider));
                    if (!addLoginResult.Succeeded)
                    {
                        return new BaseResponse<TokenDto>("External login attach failed.", HttpStatusCode.Conflict)
                        {
                            Errors = addLoginResult.Errors.Select(e => e.Description).ToList(),
                            IsSuccess = false
                        };
                    }
                }

                if (!user.EmailConfirmed)
                    user.EmailConfirmed = true;

                await AuthorizationSyncHelper.AssignRoleAndSyncUserClaimsAsync(_userManager, _roleManager, user, RoleTemplates.Buyer);

                user.LastLoginDate = nowUtc;
                await _userManager.UpdateAsync(user);
            }

            if (isNewUser)
            {
                try
                {
                    await _publishEndpoint.Publish(new EmailNotificationEvent(
                        user!.Id,
                        user.Email!,
                        "Buynity — Facebook ilə qeydiyyat tamamlandı 🎉",
                        $@"
                  <h2 style='margin:0 0 8px'>Xoş gəldiniz, {System.Net.WebUtility.HtmlEncode(user.FullName)}!</h2>
                  <p>Facebook hesabınızla <b>Buynity</b>-də qeydiyyat tamamlandı.</p>
                  <ul>
                    <li>Yeni və 2-ci əl məhsulları asanlıqla yerləşdirin</li>
                    <li>Favoritlərə əlavə edin, qiymət bildirişləri alın</li>
                    <li>Satıcı profillərini izləyin</li>
                  </ul>
                  <p>Uğurlar və bol satışlar!<br/>— Buynity Komandası</p>",
                        user.FullName,
                        user.UserName,
                        user.ProfilePicture,
                        UseHtmlTemplate: true
                    ));
                }
                catch { /* Serilog ilə logla */ }
            }

            var token = await _tokenService.CreateTokenAsync(user!);
            return BaseResponse<TokenDto>.CreateSuccess(token, "Facebook login successful.", HttpStatusCode.OK);
        }


        private static string BuildUniqueUsernameFromEmailLocalPart(IEnumerable<AppUser> allUsers, string email)
        {
            var baseUserName = email.Split('@')[0]
                .Replace("+", "", StringComparison.Ordinal)
                .Replace(".", "", StringComparison.Ordinal);

            if (string.IsNullOrWhiteSpace(baseUserName))
                baseUserName = "user";

            string candidate = baseUserName;
            int suffix = 0;
            var existing = new HashSet<string>(allUsers.Select(u => u.UserName), StringComparer.OrdinalIgnoreCase);

            while (existing.Contains(candidate))
                candidate = $"{baseUserName}{(++suffix).ToString(CultureInfo.InvariantCulture)}";

            return candidate;
        }

        private static string CreateAppSecretProof(string accessToken, string appSecret)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(accessToken));
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }
    }
}
