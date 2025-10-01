using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using OnlineShppingSystem.Application.Shared.Settings;
using OnlineShppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Infrastructure.Security;
public sealed class AppClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<AppUser, IdentityRole<Guid>>
{
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public AppClaimsPrincipalFactory(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
    {
        _roleManager = roleManager;
    }

    public override async Task<ClaimsPrincipal> CreateAsync(AppUser user)
    {
        var principal = await base.CreateAsync(user);
        var identity = (ClaimsIdentity)principal.Identity!;

        var roleNames = await UserManager.GetRolesAsync(user);
        foreach (var roleName in roleNames)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null) continue;

            var roleClaims = await _roleManager.GetClaimsAsync(role);
            foreach (var c in roleClaims.Where(c => c.Type == Permissions.ClaimType))
                identity.AddClaim(c);
        }

        return principal;
    }
}
