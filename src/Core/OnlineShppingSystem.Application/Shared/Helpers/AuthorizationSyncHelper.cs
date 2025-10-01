using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using OnlineShppingSystem.Application.Authorization;
using OnlineShppingSystem.Application.Shared.Settings;
using OnlineShppingSystem.Domain.Entities;

namespace OnlineShppingSystem.Application.Shared.Helpers;

public static class AuthorizationSyncHelper
{
    public static async Task EnsureRoleExistsAsync(RoleManager<IdentityRole<Guid>> roleManager, string roleName)
    {
        if (await roleManager.FindByNameAsync(roleName) is null)
        {
            var created = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            if (!created.Succeeded)
                throw new InvalidOperationException($"Role '{roleName}' create failed: {string.Join(", ", created.Errors.Select(x => x.Description))}");
        }
    }

    public static async Task EnsureRoleHasTemplatePermissionsAsync(RoleManager<IdentityRole<Guid>> roleManager, string roleName)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role is null) return;

        if (!RoleTemplates.ByRole.TryGetValue(roleName, out var perms) || perms is null) return;

        var existing = (await roleManager.GetClaimsAsync(role))
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var p in perms)
        {
            if (!existing.Contains(p))
            {
                var res = await roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, p));
                if (!res.Succeeded)
                    throw new InvalidOperationException($"Add role claim '{p}' failed: {string.Join(", ", res.Errors.Select(e => e.Description))}");
            }
        }
    }

    public static async Task AssignRoleAndSyncUserClaimsAsync(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        AppUser user,
        string roleName)
    {
        await EnsureRoleExistsAsync(roleManager, roleName);
        await EnsureRoleHasTemplatePermissionsAsync(roleManager, roleName);

        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            var add = await userManager.AddToRoleAsync(user, roleName);
            if (!add.Succeeded)
                throw new InvalidOperationException($"Assign role failed: {string.Join(", ", add.Errors.Select(e => e.Description))}");
        }

        await SyncUserClaimsFromCurrentRolesAsync(userManager, roleManager, user);
    }

    public static async Task SyncUserClaimsFromCurrentRolesAsync(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        AppUser user)
    {
        var currentClaims = await userManager.GetClaimsAsync(user);
        var permClaims = currentClaims.Where(c => c.Type == Permissions.ClaimType).ToList();

        foreach (var c in permClaims)
        {
            var rm = await userManager.RemoveClaimAsync(user, c);
            if (!rm.Succeeded)
                throw new InvalidOperationException($"Remove claim failed: {string.Join(", ", rm.Errors.Select(e => e.Description))}");
        }

        var roles = await userManager.GetRolesAsync(user);
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in roles)
        {
            var role = await roleManager.FindByNameAsync(r);
            if (role is null) continue;

            var roleClaims = await roleManager.GetClaimsAsync(role);
            foreach (var rc in roleClaims.Where(x => x.Type == Permissions.ClaimType))
                set.Add(rc.Value);
        }

        foreach (var p in set)
        {
            var add = await userManager.AddClaimAsync(user, new Claim(Permissions.ClaimType, p));
            if (!add.Succeeded)
                throw new InvalidOperationException($"Add claim failed: {string.Join(", ", add.Errors.Select(e => e.Description))}");
        }
    }
}
