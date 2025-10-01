using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OnlineShppingSystem.Application.Shared.Settings;
using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Application.Shared.Settings;

namespace OnlineShoppingSystem.Infrastructure.Services;
public static class IdentitySeeder
{
   
    private const string AdminRoleName = "Admin";          
    private const string ModeratorRoleName = "Moderator";
    private const string SellerRoleName = "Seller";
    private const string StoreSellerRoleName = "StoreSeller";
    private const string BuyerRoleName = "Buyer";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var seedOpt = scope.ServiceProvider.GetRequiredService<IOptions<IdentitySeedSettings>>().Value ?? new IdentitySeedSettings();

        if (!seedOpt.Enabled)
            return;

        
        var rolesToSeed = (seedOpt.SeedRoles && seedOpt.Roles is { Length: > 0 })
            ? seedOpt.Roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            : Array.Empty<string>();

        foreach (var roleName in rolesToSeed)
            await EnsureRoleAsync(roleManager, roleName);

       

       
        if (await RoleExistsAsync(roleManager, ModeratorRoleName))
        {
            await EnsureRoleClaimsAsync(roleManager, ModeratorRoleName, Permissions.SupportChat.All);
        }

        
        if (await RoleExistsAsync(roleManager, SellerRoleName))
        {
            await EnsureRoleClaimsAsync(roleManager, SellerRoleName, new[]
            {
                Permissions.Products.Create,
                Permissions.Products.CreateSecondHand,
                Permissions.Products.Update,
                Permissions.Products.Delete,
                Permissions.Products.Restore,
                Permissions.Products.ReadMy,
                Permissions.Products.ReadById
            });
        }

        
        if (await RoleExistsAsync(roleManager, StoreSellerRoleName))
        {
            await EnsureRoleClaimsAsync(roleManager, StoreSellerRoleName, new[]
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
        }

        
        if (await RoleExistsAsync(roleManager, BuyerRoleName))
        {
            
        }

        
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole<Guid>> roleManager, string roleName)
    {
        var existing = await roleManager.FindByNameAsync(roleName);
        if (existing is null)
        {
            var res = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            if (!res.Succeeded)
                throw new Exception($"Rol yaradıla bilmədi: {roleName} -> " +
                                    string.Join(", ", res.Errors.Select(e => $"{e.Code}:{e.Description}")));
        }
    }

    private static async Task<bool> RoleExistsAsync(RoleManager<IdentityRole<Guid>> roleManager, string roleName)
        => (await roleManager.FindByNameAsync(roleName)) is not null;

    private static async Task EnsureRoleClaimsAsync(
        RoleManager<IdentityRole<Guid>> roleManager,
        string roleName,
        IEnumerable<string> permissions)
    {
        var role = await roleManager.FindByNameAsync(roleName)
                  ?? throw new Exception($"{roleName} rolu tapılmadı.");

        var existing = (await roleManager.GetClaimsAsync(role))
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var perm in permissions.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!existing.Contains(perm))
            {
                var add = await roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, perm));
                if (!add.Succeeded)
                    throw new Exception($"{roleName} roluna permission '{perm}' əlavə edilə bilmədi: " +
                                        string.Join(", ", add.Errors.Select(e => e.Description)));
            }
        }
    }
}
