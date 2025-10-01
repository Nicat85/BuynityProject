using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineShppingSystem.Application.Abstracts.Services;
using OnlineShppingSystem.Application.DTOs.AuthDtos;
using OnlineShppingSystem.Application.Shared.Helpers;
using OnlineShppingSystem.Application.Shared.Settings;
using OnlineShppingSystem.Domain.Entities;
using System.Security.Claims;

namespace OnlineShoppingSystem.WebApplication.Controllers;

[Route("api/users")]
[ApiController]
[Authorize] 
public class UserController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<UserController> _logger;
    private readonly UserManager<AppUser> _userManager;
    private readonly IWebHostEnvironment _env;
    private readonly IProfileService _profileService;

    public UserController(IAuthService authService,
        ILogger<UserController> logger,
        UserManager<AppUser> userManager,
        IWebHostEnvironment env,
        IProfileService profileService)
    {
        _authService = authService;
        _logger = logger;
        _userManager = userManager;
        _env = env;
        _profileService = profileService;
    }

    [HttpGet("profile")]
    [Authorize(Policy = Permissions.Users.ReadProfile)]
    public async Task<IActionResult> GetProfile()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            _logger.LogError("UserId is missing or invalid in token.");
            return Unauthorized();
        }

        var result = await _authService.GetProfileAsync(userId);
        if (!result.IsSuccess) return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("profile")]
    [Authorize(Policy = Permissions.Users.UpdateProfile)]
    public async Task<IActionResult> EditProfile(EditProfileDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var response = await _authService.EditProfileAsync(Guid.Parse(userId), request);
        if (!response.IsSuccess)
            return BadRequest(response.Message);

        return Ok(response.Data);
    }

    [HttpGet]
    [Authorize(Policy = Permissions.Users.ReadAll)]
    public async Task<IActionResult> GetAllUsers()
    {
        var result = await _authService.GetAllUsersAsync();
        if (!result.IsSuccess) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("upload-profile-picture")]
    [Authorize(Policy = Permissions.Users.UploadProfilePicture)]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
            return Unauthorized("User not authenticated");

        if (!Guid.TryParse(userIdClaim, out var userId))
            return BadRequest("Invalid user ID");

        var result = await _profileService.UploadProfilePictureAsync(userId, file);

        if (!result.IsSuccess)
            return BadRequest(result.Message);

        return Ok(result.Data);
    }
}
