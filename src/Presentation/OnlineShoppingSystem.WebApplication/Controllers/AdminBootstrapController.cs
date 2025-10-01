using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using OnlineShoppingSystem.Infrastructure.Security;
using OnlineShppingSystem.Application.Shared;
using OnlineShppingSystem.Application.Shared.Settings;
using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Application.Shared.Settings;
using System.Net;
using System.Security.Claims;

namespace OnlineShoppingSystem.WebApplication.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/admin/bootstrap")]
[EnableRateLimiting("BootstrapTight")]
public sealed class AdminBootstrapController : ControllerBase
{
    private readonly IAdminBootstrapGuard _guard;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IOptions<IdentitySeedSettings> _seed;

    public AdminBootstrapController(
        IAdminBootstrapGuard guard,
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IOptions<IdentitySeedSettings> seed)
    {
        _guard = guard;
        _userManager = userManager;
        _roleManager = roleManager;
        _seed = seed;
    }

    public sealed class BootstrapAdminRequest
    {
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string FullName { get; set; } = "Primary Admin";
        public bool Require2FA { get; set; } = true;
    }

    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseResponse>> Create(
        [FromBody] BootstrapAdminRequest req,
        [FromHeader(Name = "X-Admin-Bootstrap-Token")] string bootstrapToken) 
    {
        
        if (!await _guard.IsEnabledAsync())
            return Unauthorized(BaseResponse.Fail("Admin bootstrap disabled (already used or not enabled)."));

        
        var remoteIp = HttpContext.Connection.RemoteIpAddress;
        if (!await _guard.IsIpAllowedAsync(remoteIp))
            return Unauthorized(BaseResponse.Fail($"IP not allowed: {remoteIp}"));

        
        if (string.IsNullOrWhiteSpace(bootstrapToken) || !await _guard.IsValidTokenAsync(bootstrapToken))
            return Unauthorized(BaseResponse.Fail("Invalid bootstrap token."));

        
        var adminRole = await EnsureRoleAsync("ADMIN", Permissions.GetAll()); 
        await EnsureRoleAsync("MODERATOR", Permissions.SupportChat.All);
        await EnsureRoleAsync("Seller", new[]
        {
            Permissions.Products.Create,
            Permissions.Products.CreateSecondHand,
            Permissions.Products.Update,
            Permissions.Products.Delete,
            Permissions.Products.Restore,
            Permissions.Products.ReadMy,
            Permissions.Products.ReadById
        });
        await EnsureRoleAsync("StoreSeller", new[]
        {
            Permissions.Products.Create,
            Permissions.Products.CreateSecondHand,
            Permissions.Products.CreateStore,
            Permissions.Products.Update,
            Permissions.Products.Delete,
            Permissions.Products.Restore,
            Permissions.Products.ReadMy,
            Permissions.Products.ReadById
        });

       
        var email = (req.Email ?? "").Trim();
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(BaseResponse.Fail("Email required."));

        if (await _userManager.FindByEmailAsync(email) is not null)
            return BadRequest(BaseResponse.Fail("Email already exists."));

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            FullName = string.IsNullOrWhiteSpace(req.FullName) ? "Primary Admin" : req.FullName.Trim(),
            EmailConfirmed = true
        };

        var create = await _userManager.CreateAsync(user, req.Password);
        if (!create.Succeeded)
            return BadRequest(BaseResponse.Fail(string.Join(", ", create.Errors.Select(e => e.Description))));

        var addToRole = await _userManager.AddToRoleAsync(user, adminRole.Name!);
        if (!addToRole.Succeeded)
            return BadRequest(BaseResponse.Fail(string.Join(", ", addToRole.Errors.Select(e => e.Description))));

        
        if (req.Require2FA)
        {
            await _userManager.SetTwoFactorEnabledAsync(user, true);
            await _userManager.ResetAuthenticatorKeyAsync(user);
        }

        
        var consumed = await _guard.ConsumeOnceAsync();
        if (!consumed)
            return Unauthorized(BaseResponse.Fail("Bootstrap already consumed."));

        return Ok(BaseResponse.Success("Admin user created successfully."));
    }

   
    private async Task<IdentityRole<Guid>> EnsureRoleAsync(string roleName, IEnumerable<string> permissions)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            var cr = await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            if (!cr.Succeeded)
                throw new InvalidOperationException($"Role create failed: {string.Join(", ", cr.Errors.Select(e => e.Description))}");
            role = await _roleManager.FindByNameAsync(roleName)
                   ?? throw new InvalidOperationException($"Role '{roleName}' cannot be loaded after creation.");
        }

        var existing = (await _roleManager.GetClaimsAsync(role))
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var p in permissions.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (existing.Contains(p)) continue;
            var add = await _roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, p));
            if (!add.Succeeded)
                throw new InvalidOperationException($"Add permission '{p}' to role '{roleName}' failed: {string.Join(", ", add.Errors.Select(e => e.Description))}");
        }

        return role;
    }
}
