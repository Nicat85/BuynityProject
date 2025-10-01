using Google.Apis.Auth;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OnlineShppingSystem.Application.Abstracts.Services;
using OnlineShppingSystem.Application.Authorization;
using OnlineShppingSystem.Application.DTOs.AuthDtos;
using OnlineShppingSystem.Application.DTOs.TokenDto;
using OnlineShppingSystem.Application.Shared;
using OnlineShppingSystem.Application.Shared.Helpers;
using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Application.Events;
using System.Globalization;
using System.Net;

namespace OnlineShoppingSystem.Infrastructure.Services;

public class GoogleService : IGoogleService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly IPublishEndpoint _publishEndpoint;

    public GoogleService(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ITokenService tokenService,
        IConfiguration configuration,
        IPublishEndpoint publishEndpoint)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _configuration = configuration;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<BaseResponse<TokenDto>> GoogleLoginAsync(GoogleAuthRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request?.IdToken))
            return BaseResponse<TokenDto>.Fail("IdToken is required.", HttpStatusCode.BadRequest);

        var payload = await ValidateGoogleIdTokenAsync(request.IdToken);
        if (payload == null)
            return BaseResponse<TokenDto>.Fail("Google token is invalid or expired.", HttpStatusCode.BadRequest);

        if (payload.EmailVerified == false)
            return BaseResponse<TokenDto>.Fail("Google email is not verified.", HttpStatusCode.BadRequest);

        var email = payload.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
            return BaseResponse<TokenDto>.Fail("Email not found in Google profile.", HttpStatusCode.BadRequest);

        var name = !string.IsNullOrWhiteSpace(payload.Name)
            ? payload.Name.Trim()
            : $"{payload.GivenName} {payload.FamilyName}".Trim();

        var provider = "Google";
        var providerKey = payload.Subject;
        var nowUtc = DateTime.UtcNow;

        var linkedUser = await _userManager.FindByLoginAsync(provider, providerKey);
        AppUser? user = linkedUser;

        if (user == null)
        {
            var normalizedEmail = email.ToLowerInvariant();
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
            var allUsers = await _userManager.Users.ToListAsync();
            var userName = BuildUniqueUsernameFromEmailLocalPart(allUsers, email);
            var displayName = string.IsNullOrWhiteSpace(name) ? userName : name;
            var avatarText = AvatarHelper.GenerateAvatarText(displayName);

            user = new AppUser
            {
                Email = email,
                UserName = userName,
                FullName = displayName,
                EmailConfirmed = true,
                LastLoginDate = nowUtc,
                IsDeleted = false,
                AvatarText = avatarText,
                ProfilePicture = !string.IsNullOrWhiteSpace(payload.Picture)
                                  ? payload.Picture
                                  : $"https://ui-avatars.com/api/?name={avatarText}&background=random&size=360"
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                return new BaseResponse<TokenDto>("User could not be created.", HttpStatusCode.InternalServerError)
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

            if (!user.EmailConfirmed && payload.EmailVerified)
            {
                user.EmailConfirmed = true;
            }

           
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
                    "Buynity — Google ilə qeydiyyat tamamlandı 🎉",
                    $@"
                     <h2 style='margin:0 0 8px'>Xoş gəldiniz, {System.Net.WebUtility.HtmlEncode(user.FullName)}!</h2>
                     <p>Google hesabınızla <b>Buynity</b>-də qeydiyyat tamamlandı.</p>
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
            catch { /* log */ }
        }

        var token = await _tokenService.CreateTokenAsync(user!);
        return BaseResponse<TokenDto>.CreateSuccess(token, "Google login successful.", HttpStatusCode.OK);
    }

    public async Task<BaseResponse<string>> SetPasswordAsync(Guid userId, string newPassword)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return BaseResponse<string>.Fail("User not found.", HttpStatusCode.NotFound);

        if (!user.EmailConfirmed)
            return BaseResponse<string>.Fail("Email is not confirmed.", HttpStatusCode.BadRequest);

        var hasPassword = await _userManager.HasPasswordAsync(user);
        if (hasPassword)
            return BaseResponse<string>.Fail("Password already set. Use Change Password.", HttpStatusCode.BadRequest);

        var result = await _userManager.AddPasswordAsync(user, newPassword);
        if (!result.Succeeded)
        {
            return new BaseResponse<string>("Failed to set password.", HttpStatusCode.BadRequest)
            {
                Errors = result.Errors.Select(e => e.Description).ToList(),
                IsSuccess = false
            };
        }

        return BaseResponse<string>.CreateSuccess("Password set successfully.", "OK", HttpStatusCode.OK);
    }

    private async Task<GoogleJsonWebSignature.Payload?> ValidateGoogleIdTokenAsync(string idToken)
    {
        var audiences = new List<string>();
        var webClientId = _configuration["GoogleAuthSettings:ClientId"];
        var androidClientId = _configuration["GoogleAuthSettings:AndroidClientId"];
        var iosClientId = _configuration["GoogleAuthSettings:iOSClientId"];
        if (!string.IsNullOrWhiteSpace(webClientId)) audiences.Add(webClientId);
        if (!string.IsNullOrWhiteSpace(androidClientId)) audiences.Add(androidClientId);
        if (!string.IsNullOrWhiteSpace(iosClientId)) audiences.Add(iosClientId);

        audiences.Add("407408718192.apps.googleusercontent.com");

        var settings = new GoogleJsonWebSignature.ValidationSettings { Audience = audiences };

        try
        {
            return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        }
        catch
        {
            return null;
        }
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
}
