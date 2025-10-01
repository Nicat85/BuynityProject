using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OnlineShoppingSystem.Infrastructure.Services;
using OnlineShoppingSystem.Persistence.Contexts;
using OnlineShppingSystem.Application.Abstracts.Services;
using OnlineShppingSystem.Application.Authorization;
using OnlineShppingSystem.Application.DTOs.AuthDtos;
using OnlineShppingSystem.Application.DTOs.EmailDtos;
using OnlineShppingSystem.Application.DTOs.TokenDto;
using OnlineShppingSystem.Application.Shared;
using OnlineShppingSystem.Application.Shared.Helpers;
using OnlineShppingSystem.Application.Shared.Settings;
using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.Events;
using System.Net;
using System.Text.RegularExpressions;

namespace OnlineShoppingSystem.Persistence.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly OnlineShoppingSystemDbContext _context;
    private readonly ILogger<AuthService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IWebHostEnvironment _env;
    private readonly IProfileService _profileService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly RedisRefreshTokenService _redisTokenService;
    private readonly IAccessTokenService _accessTokenService;
    private readonly JWTSettings _jwtSettings;

    public AuthService(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        SignInManager<AppUser> signInManager,
        ITokenService tokenService,
        IConfiguration configuration,
        OnlineShoppingSystemDbContext context,
        ILogger<AuthService> logger,
        HttpClient httpClient,
        IWebHostEnvironment env,
        IProfileService profileService,
        IPublishEndpoint publishEndpoint,
        RedisRefreshTokenService redisTokenService,
        IAccessTokenService accessTokenService,
        JWTSettings jwtSettings)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _configuration = configuration;
        _context = context;
        _logger = logger;
        _httpClient = httpClient;
        _env = env;
        _profileService = profileService;
        _publishEndpoint = publishEndpoint;
        _redisTokenService = redisTokenService;
        _accessTokenService = accessTokenService;
        _jwtSettings = jwtSettings;
    }

    public async Task<BaseResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto request, IFormFile? profilePictureFile = null)
    {
        var email = request.Email?.Trim();
        var fullName = request.FullName?.Trim();
        var phone = request.PhoneNumber?.Trim();
        var password = request.Password;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return BaseResponse<AuthResponseDto>.Fail("Email və Password tələb olunur.", HttpStatusCode.BadRequest);

        if (await _userManager.FindByEmailAsync(email) is not null)
            return BaseResponse<AuthResponseDto>.Fail("Email already registered.", HttpStatusCode.BadRequest);

        if (!string.IsNullOrWhiteSpace(phone))
        {
            var phoneExists = await _userManager.Users.AnyAsync(u => u.PhoneNumber == phone);
            if (phoneExists)
                return BaseResponse<AuthResponseDto>.Fail("This phone number has already been used.", HttpStatusCode.BadRequest);
        }

        string generatedUsername;
        do
        {
            generatedUsername = $"User{Random.Shared.Next(10000, 99999)}";
        }
        while (await _userManager.FindByNameAsync(generatedUsername) != null);

        var avatarText = AvatarHelper.GenerateAvatarText(fullName);
        var defaultAvatarUrl = $"https://ui-avatars.com/api/?name={avatarText}&background=random&size=360";

        var user = new AppUser
        {
            UserName = generatedUsername,
            Email = email,
            FullName = fullName,
            PhoneNumber = phone,
            EmailConfirmed = false,
            IsDeleted = false,
            AvatarText = avatarText,
            ProfilePicture = defaultAvatarUrl,
            LastLoginDate = DateTime.UtcNow
        };

        var create = await _userManager.CreateAsync(user, password);
        if (!create.Succeeded)
            return BaseResponse<AuthResponseDto>.Fail(create.Errors.Select(e => e.Description));

        if (profilePictureFile is { Length: > 0 })
        {
            var uploadResult = await _profileService.UploadProfilePictureAsync(user.Id, profilePictureFile);
            if (uploadResult.IsSuccess && !string.IsNullOrWhiteSpace(uploadResult.Data))
            {
                user.ProfilePicture = uploadResult.Data;
                var upd = await _userManager.UpdateAsync(user);
                if (!upd.Succeeded)
                    return BaseResponse<AuthResponseDto>.Fail(upd.Errors.Select(e => e.Description));
            }
        }

       
        await AuthorizationSyncHelper.AssignRoleAndSyncUserClaimsAsync(_userManager, _roleManager, user, RoleTemplates.Buyer);

       
        var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationLink =
            $"{_configuration["AppSettings:FrontendUrl"]}/confirm-email" +
            $"?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(emailToken)}";

        await _publishEndpoint.Publish(new EmailNotificationEvent(
            user.Id,
            user.Email!,
            "Emailinizi təsdiqləyin",
            $"Zəhmət olmasa, emailinizi təsdiqləmək üçün <a href='{confirmationLink}'>buraya klikləyin</a>.",
            user.FullName,
            user.UserName,
            user.ProfilePicture,
            UseHtmlTemplate: true
        ));

        await _publishEndpoint.Publish(new EmailNotificationEvent(
            user.Id,
            user.Email!,
            "Buynity -ə xoş gəlmisiniz!",
            $@"
        <b>Buynity</b> platformasına xoş gəlmisiniz! 🎉<br/><br/>
        Əla ikinci əl təklifləri araşdıra, məhsullar sata və rahat alış-veriş edə bilərsiniz.<br/><br/>
        Hər hansı bir sualınız olarsa, bizə istənilən zaman yazın.<br/><br/>
        Uğurlar!<br/>
        — Buynity Komandası",
            user.FullName,
            user.UserName,
            user.ProfilePicture,
            UseHtmlTemplate: true
        ));

        var token = await _tokenService.CreateTokenAsync(user);

        return BaseResponse<AuthResponseDto>.CreateSuccess(
            new AuthResponseDto(token.AccessToken, token.RefreshToken, token.Expires),
            "Registration successful"
        );
    }

    public async Task<BaseResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request)
    {
        var input = request.EmailOrPhoneNumber?.Trim();
        if (string.IsNullOrWhiteSpace(input))
            return BaseResponse<AuthResponseDto>.Fail("Email or phone number is required.", HttpStatusCode.BadRequest);

        AppUser? user;
        if (input.Contains("@"))
        {
            var norm = input.ToLowerInvariant();
            user = await _userManager.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == norm);
        }
        else
        {
            var norm = input.ToLowerInvariant();
            user = await _userManager.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u =>
                    u.PhoneNumber == input ||
                    (u.UserName != null && u.UserName.ToLower() == norm));
        }

        if (user == null)
            return BaseResponse<AuthResponseDto>.Fail("Invalid credentials. User not found.", HttpStatusCode.BadRequest);

        
        if (user.IsDeleted)
        {
            if (user.DeletedAt.HasValue && (DateTime.UtcNow - user.DeletedAt.Value).TotalDays <= 30)
            {
                user.IsDeleted = false;
                user.DeletedAt = null;
                await _userManager.UpdateAsync(user);

                var recoveryMessage = $@"
                  {user.FullName}, hesabınız {DateTime.UtcNow:dd MMMM yyyy} tarixində uğurla <b>bərpa edildi</b>! 🎉<br/><br/>
                   Sizi yenidən görmək çox xoşdur.<br/>
                   Əgər bu siz deyilsinizsə, zəhmət olmasa dərhal <a href='{_configuration["AppSettings:FrontendUrl"]}/reset-password'>şifrənizi sıfırlayın</a>.<br/><br/>
                 — Buynity Komandası";

                await _publishEndpoint.Publish(new EmailNotificationEvent(
                    user.Id,
                    user.Email!,
                    "Hesabınız bərpa olundu",
                    recoveryMessage,
                    user.FullName,
                    user.UserName,
                    user.ProfilePicture,
                    UseHtmlTemplate: true
                ));
            }
            else
            {
                return BaseResponse<AuthResponseDto>.Fail("Your account is deactivated.", HttpStatusCode.Forbidden);
            }
        }

        if (!user.EmailConfirmed)
            return BaseResponse<AuthResponseDto>.Fail("Email is not confirmed.", HttpStatusCode.Unauthorized);

        
        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (signInResult.IsLockedOut)
            return BaseResponse<AuthResponseDto>.Fail("Account is locked due to multiple failed attempts. Please try again later.", HttpStatusCode.Locked);
        if (!signInResult.Succeeded)
            return BaseResponse<AuthResponseDto>.Fail("Invalid credentials. Wrong password.", HttpStatusCode.BadRequest);

        user.LastLoginDate = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var token = await _tokenService.CreateTokenAsync(user);
        var response = new AuthResponseDto(token.AccessToken, token.RefreshToken, token.Expires);
        return BaseResponse<AuthResponseDto>.CreateSuccess(response, "Login successful.");
    }

    public async Task<BaseResponse<TokenDto>> RefreshTokenAsync(string refreshToken, string oldAccessToken)
    {
        var userId = await _redisTokenService.ValidateAsync(refreshToken);
        if (userId == null)
            return BaseResponse<TokenDto>.Fail("Invalid or expired refresh token", HttpStatusCode.Unauthorized);

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return BaseResponse<TokenDto>.Fail("User not found", HttpStatusCode.NotFound);

        
        await _redisTokenService.RemoveAsync(refreshToken);

       
        if (!string.IsNullOrWhiteSpace(oldAccessToken))
        {
            var ttl = GetAccessTokenTtl(oldAccessToken);
            await _accessTokenService.BlacklistAsync(oldAccessToken, ttl);
        }

        var newToken = await _tokenService.CreateTokenAsync(user);

        return BaseResponse<TokenDto>.CreateSuccess(new TokenDto
        {
            AccessToken = newToken.AccessToken,
            RefreshToken = newToken.RefreshToken,
            Expires = newToken.Expires
        }, "Token refreshed successfully");
    }

    public async Task<BaseResponse<List<UserDto>>> GetAllUsersAsync()
    {
        var users = _userManager.Users.ToList();
        var userDtos = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);
            var permissions = claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToList();

            userDtos.Add(new UserDto(
                user.Id,
                user.FullName ?? string.Empty,
                user.UserName ?? string.Empty,
                user.Email ?? string.Empty,
                user.PhoneNumber ?? string.Empty,
                user.EmailConfirmed,
                roles.ToList(),
                permissions,
                user.IsDeleted,
                user.ProfilePicture ?? string.Empty,
                user.AvatarText ?? string.Empty,
                user.Bio ?? string.Empty,
                user.Address ?? string.Empty
            ));
        }

        return BaseResponse<List<UserDto>>.CreateSuccess(userDtos);
    }

    public async Task<BaseResponse<UserDto>> GetProfileAsync(Guid userId)
    {
        try
        {
            var user = await _userManager.Users.Where(u => u.Id == userId && !u.IsDeleted).FirstOrDefaultAsync();
            if (user == null)
            {
                _logger.LogWarning($"User not found: {userId}");
                return BaseResponse<UserDto>.Fail("User not found.", HttpStatusCode.NotFound);
            }

            var roles = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);
            var permissions = claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToList();

            var userDto = new UserDto(
                user.Id,
                user.FullName ?? string.Empty,
                user.UserName ?? string.Empty,
                user.Email ?? string.Empty,
                user.PhoneNumber ?? string.Empty,
                user.EmailConfirmed,
                roles.ToList(),
                permissions,
                user.IsDeleted,
                user.ProfilePicture ?? string.Empty,
                user.AvatarText ?? string.Empty,
                user.Bio ?? string.Empty,
                user.Address ?? string.Empty
            );

            return BaseResponse<UserDto>.CreateSuccess(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving user profile.");
            return BaseResponse<UserDto>.Fail("Internal server error.", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<BaseResponse<string>> ConfirmEmailAsync(ConfirmEmailRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return BaseResponse<string>.Fail("User not found.", HttpStatusCode.NotFound);

        if (user.EmailConfirmed)
            return BaseResponse<string>.CreateSuccess(null, "Your email is already confirmed.");

        var decodedToken = WebUtility.UrlDecode(request.Token);
        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return BaseResponse<string>.Fail(errors);
        }

        return BaseResponse<string>.Success("Email confirmed successfully.");
    }

    public async Task<BaseResponse<string>> ForgotPasswordAsync(ForgotPasswordRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BaseResponse<string>.Fail("Email is required.", HttpStatusCode.BadRequest);

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
            return BaseResponse<string>.Success("If an account with this email exists, a password reset link has been sent.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetLink =
            $"{_configuration["AppSettings:ClientURL"]}/reset-password" +
            $"?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";

        await _publishEndpoint.Publish(new EmailNotificationEvent(
            user.Id,
            user.Email!,
            "Reset Password",
            $"Şifrənizi sıfırlamaq üçün <a href='{resetLink}'>bu linkə</a> klikləyin.",
            user.FullName,
            user.UserName,
            user.ProfilePicture,
            UseHtmlTemplate: true
        ));

        return BaseResponse<string>.Success("Password reset link has been sent to your email.");
    }

    public async Task<BaseResponse<string>> ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Token) ||
            string.IsNullOrWhiteSpace(request.NewPassword))
            return BaseResponse<string>.Fail("Email, token and new password are required.", HttpStatusCode.BadRequest);

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return BaseResponse<string>.Fail("User not found.", HttpStatusCode.NotFound);

        
        if (!string.IsNullOrEmpty(user.PasswordHash))
        {
            var hasher = new PasswordHasher<AppUser>();
            var verify = hasher.VerifyHashedPassword(user, user.PasswordHash, request.NewPassword);
            if (verify == PasswordVerificationResult.Success)
                return BaseResponse<string>.Fail("New password cannot be the same as the current password.");
        }

        var decodedToken = System.Net.WebUtility.UrlDecode(request.Token);
        var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);
        if (!result.Succeeded)
            return BaseResponse<string>.Fail(result.Errors.Select(e => e.Description));

        return BaseResponse<string>.Success("Password has been reset successfully.");
    }

    public async Task<BaseResponse<UserDto>> EditProfileAsync(Guid userId, EditProfileDto request)
    {
        var user = await _userManager.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return BaseResponse<UserDto>.Fail("User not found.", HttpStatusCode.NotFound);

       
        if (!string.IsNullOrWhiteSpace(request.UserName) && request.UserName != user.UserName)
        {
            var existsUserName = await _userManager.Users.AnyAsync(u => u.UserName == request.UserName && u.Id != userId);
            if (existsUserName)
                return BaseResponse<UserDto>.Fail("Username is already taken.", HttpStatusCode.Conflict);
        }

        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
        {
            var existsEmail = await _userManager.Users.AnyAsync(u => u.Email == request.Email && u.Id != userId);
            if (existsEmail)
                return BaseResponse<UserDto>.Fail("Email is already in use.", HttpStatusCode.Conflict);
        }

        
        string? normalizedPhone = null;
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            normalizedPhone = NormalizePhone(request.PhoneNumber);
            if (normalizedPhone is null)
                return BaseResponse<UserDto>.Fail("Phone number format is invalid.", HttpStatusCode.BadRequest);

            if (normalizedPhone != user.PhoneNumber)
            {
                var existsPhone = await _userManager.Users.AnyAsync(u => u.PhoneNumber == normalizedPhone && u.Id != userId);
                if (existsPhone)
                    return BaseResponse<UserDto>.Fail("Phone number is already in use.", HttpStatusCode.Conflict);
            }
        }

        
        if (!string.IsNullOrWhiteSpace(request.FinCode))
        {
            var fin = request.FinCode.Trim().ToUpperInvariant();
            if (!Regex.IsMatch(fin, "^[A-Z0-9]{7}$"))
                return BaseResponse<UserDto>.Fail("FIN code is invalid. It must be 7 characters [A-Z0-9].", HttpStatusCode.BadRequest);

            if (!string.Equals(user.FinCode, fin, StringComparison.Ordinal))
            {
                var existsFin = await _userManager.Users.AnyAsync(u => u.FinCode == fin && u.Id != userId);
                if (existsFin)
                    return BaseResponse<UserDto>.Fail("FIN code is already used by another account.", HttpStatusCode.Conflict);

                user.FinCode = fin;
            }
        }

        var emailChanged = !string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email;
        var phoneChanged = normalizedPhone != null && normalizedPhone != user.PhoneNumber;

        var oldFullName = user.FullName;

        if (!string.IsNullOrWhiteSpace(request.FullName))
            user.FullName = request.FullName;

        if (!string.IsNullOrWhiteSpace(request.UserName))
            user.UserName = request.UserName;

        if (!string.IsNullOrWhiteSpace(request.Email))
            user.Email = request.Email;

        if (normalizedPhone != null)
            user.PhoneNumber = normalizedPhone;

        if (!string.IsNullOrWhiteSpace(request.Bio))
            user.Bio = request.Bio;

        if (!string.IsNullOrWhiteSpace(request.Address))
            user.Address = request.Address;

        if (!string.IsNullOrWhiteSpace(request.FullName) && request.FullName != oldFullName)
            user.AvatarText = GenerateAvatarText(request.FullName);

        if (emailChanged) user.EmailConfirmed = false;
        if (phoneChanged) user.PhoneNumberConfirmed = false;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return BaseResponse<UserDto>.Fail(updateResult.Errors.Select(e => e.Description));

        var roles = await _userManager.GetRolesAsync(user);
        var claims = await _userManager.GetClaimsAsync(user);
        var permissions = claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToList();

        var userDto = new UserDto(
            user.Id,
            user.FullName ?? string.Empty,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            user.PhoneNumber ?? string.Empty,
            user.EmailConfirmed,
            roles.ToList(),
            permissions,
            user.IsDeleted,
            user.ProfilePicture ?? string.Empty,
            user.AvatarText ?? string.Empty,
            user.Bio ?? string.Empty,
            user.Address ?? string.Empty
        );

        return BaseResponse<UserDto>.CreateSuccess(userDto);
    }
  
    private static string? NormalizePhone(string input)
    {
        
        var digits = Regex.Replace(input, "[^0-9+]", "");

        
        if (!digits.StartsWith("+"))
        {
            if (digits.StartsWith("00")) digits = "+" + digits.Substring(2);
            else if (digits.StartsWith("0")) digits = "+994" + digits.Substring(1);
            else if (digits.Length >= 12 && digits.StartsWith("994")) digits = "+" + digits; 
            else return null;
        }
        return digits;
    }

    private string GenerateAvatarText(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return string.Empty;
        if (parts.Length == 1) return parts[0][0].ToString().ToUpper();
        return (parts[0][0].ToString() + parts[1][0].ToString()).ToUpper();
    }

    public async Task<BaseResponse<string>> DeleteAccountAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return BaseResponse<string>.Fail("User not found", HttpStatusCode.NotFound);

        if (user.IsDeleted)
            return BaseResponse<string>.Fail("Account is already marked as deleted.", HttpStatusCode.BadRequest);

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);

        var emailMessage = $@"
        {user.FullName}, hesabınız {DateTime.UtcNow:dd MMMM yyyy} tarixində <b>deaktiv edildi</b>.<br/><br/>
        Bu qərarı özünüz verdinizsə, heç bir addım atmağa ehtiyac yoxdur.<br/><br/>
        Ancaq fikrinizi dəyişsəniz, növbəti <b>30 gün</b> ərzində sadəcə login olaraq hesabınızı bərpa edə bilərsiniz.<br/><br/>
        30 gündən sonra hesabınız və bütün məlumatlarınız <b>davamlı olaraq silinəcək</b>.<br/><br/>
        — Buynity Komandası";

        await _publishEndpoint.Publish(new EmailNotificationEvent(
            user.Id,
            user.Email!,
            "Hesabınız deaktiv edildi",
            emailMessage,
            user.FullName,
            user.UserName,
            user.ProfilePicture,
            UseHtmlTemplate: true
        ));

        return BaseResponse<string>.Success("Account successfully deactivated. A confirmation email has been sent.");
    }

    public async Task<BaseResponse<string>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.IsDeleted)
            return BaseResponse<string>.Fail("User not found.", HttpStatusCode.NotFound);

        
        if (dto.NewPassword == dto.CurrentPassword)
            return BaseResponse<string>.Fail("New password cannot be the same as the current password.", HttpStatusCode.BadRequest);

        var check = await _userManager.CheckPasswordAsync(user, dto.CurrentPassword);
        if (!check)
            return BaseResponse<string>.Fail("Current password is incorrect.", HttpStatusCode.Unauthorized);

       

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
        {
            var msg = string.Join("; ", result.Errors.Select(e => e.Description));
            return BaseResponse<string>.Fail($"Password change failed. {msg}", HttpStatusCode.BadRequest);
        }

        

        return BaseResponse<string>.Success("Password changed successfully.");
    }

    public async Task<BaseResponse<string>> ResendEmailConfirmationAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BaseResponse<string>.Fail("Email is required.", HttpStatusCode.BadRequest);

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
            return BaseResponse<string>.Fail("User not found.", HttpStatusCode.NotFound);

        if (user.EmailConfirmed)
            return BaseResponse<string>.Fail("Email is already confirmed.", HttpStatusCode.BadRequest);

        var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationLink =
            $"{_configuration["AppSettings:FrontendUrl"]}/confirm-email" +
            $"?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(emailToken)}";

        var emailBody = $@"
        Zəhmət olmasa emailinizi təsdiqləmək üçün <a href='{confirmationLink}'>buraya klikləyin</a>.<br/><br/>
        Əgər bu siz deyilsinizsə, bu mesajı nəzərə almayın.<br/><br/>
        — Buynity Komandası ";

        await _publishEndpoint.Publish(new EmailNotificationEvent(
            user.Id,
            user.Email!,
            "Emailinizi təsdiqləyin",
            emailBody,
            user.FullName,
            user.UserName,
            user.ProfilePicture,
            UseHtmlTemplate: true
        ));

        return BaseResponse<string>.Success("Confirmation email sent successfully.");
    }

    public async Task<BaseResponse> LogoutAsync(string refreshToken, string accessToken)
    {
        var userId = await _redisTokenService.ValidateAsync(refreshToken);
        if (userId == null)
            return BaseResponse.Fail("Refresh token is invalid or already expired", HttpStatusCode.Unauthorized);

        await _redisTokenService.RemoveAsync(refreshToken);

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            var ttl = GetAccessTokenTtl(accessToken);
            await _accessTokenService.BlacklistAsync(accessToken, ttl);
        }

        return BaseResponse.Success("Logged out successfully");
    }

    private TimeSpan GetAccessTokenTtl(string accessToken)
    {
        try
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            if (!handler.CanReadToken(accessToken))
                return TimeSpan.FromMinutes(_jwtSettings.AccessTokenExpirationMinutes);

            var jwt = handler.ReadJwtToken(accessToken);
            var expUtc = jwt.ValidTo; 
            var nowUtc = DateTime.UtcNow;

            var calculated = expUtc - nowUtc;

            
            var maxTtl = TimeSpan.FromMinutes(_jwtSettings.AccessTokenExpirationMinutes);
            if (calculated > maxTtl) calculated = maxTtl;

            
            return calculated > TimeSpan.Zero ? calculated : TimeSpan.FromSeconds(1);
        }
        catch
        {
            return TimeSpan.FromMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        }
    }
}
