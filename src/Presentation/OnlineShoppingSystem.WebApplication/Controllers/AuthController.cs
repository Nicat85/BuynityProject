using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineShoppingSystem.Persistence.Contexts;
using OnlineShppingSystem.Application.Abstracts.Services;
using OnlineShppingSystem.Application.Common.Extensions;
using OnlineShppingSystem.Application.DTOs.AuthDtos;
using OnlineShppingSystem.Application.DTOs.EmailDtos;
using OnlineShppingSystem.Application.DTOs.TokenDto;
using OnlineShppingSystem.Application.Shared.Settings;
using OnlineShppingSystem.Domain.Entities;
using System.Security.Claims;

namespace OnlineShoppingSystem.WebApplication.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly IFacebookService _facebookService;
    private readonly IGoogleService _googleService;
    private readonly UserManager<AppUser> _userManager;
    private readonly OnlineShoppingSystemDbContext _context;

    public AuthController(
        IAuthService authService,
        ILogger<AuthController> logger,
        IFacebookService facebookService,
        IGoogleService googleService,
        UserManager<AppUser> userManager,
        OnlineShoppingSystemDbContext context)
    {
        _authService = authService;
        _logger = logger;
        _facebookService = facebookService;
        _googleService = googleService;
        _userManager = userManager;
        _context = context;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var result = await _authService.RegisterAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleAuthRequestDto request)
    {
        var result = await _googleService.GoogleLoginAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost("facebook-login")]
    [AllowAnonymous]
    public async Task<IActionResult> FacebookLogin([FromBody] FacebookLoginRequestDto dto)
    {
        var result = await _facebookService.LoginWithFacebookAsync(dto.AccessToken);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
    {
        var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        var result = await _authService.RefreshTokenAsync(dto.RefreshToken, accessToken);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost("resend-email-confirmation")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendEmailConfirmation([FromBody] ResendEmailConfirmationRequestDto request)
    {
        var result = await _authService.ResendEmailConfirmationAsync(request.Email);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequestDto request)
    {
        var result = await _authService.ConfirmEmailAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        var result = await _authService.ForgotPasswordAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        var result = await _authService.ResetPasswordAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }


    [HttpPost("change-password")]
    [Authorize(Policy = Permissions.Auth.ChangePassword)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var response = await _authService.ChangePasswordAsync(userId, dto);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpPost("logout")]
    [Authorize(Policy = Permissions.Auth.Logout)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto dto)
    {
        var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        var result = await _authService.LogoutAsync(dto.RefreshToken, accessToken);
        return StatusCode((int)result.StatusCode, result);
    }

    [Authorize(Policy = Permissions.Auth.DeleteAccount)]
    [HttpDelete("delete-account")]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = User.GetUserId();
        var result = await _authService.DeleteAccountAsync(userId);
        return StatusCode((int)result.StatusCode, result);
    }
}
