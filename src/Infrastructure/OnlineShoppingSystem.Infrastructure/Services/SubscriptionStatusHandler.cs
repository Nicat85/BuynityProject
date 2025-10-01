using Microsoft.AspNetCore.Identity;
using OnlineShppingSystem.Application.Abstracts.Services;
using OnlineShppingSystem.Application.Authorization;
using OnlineShppingSystem.Application.Shared.Helpers;
using OnlineShppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Infrastructure.Services;

public sealed class SubscriptionStatusHandler : ISubscriptionStatusHandler
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public SubscriptionStatusHandler(UserManager<AppUser> userManager,
                                     RoleManager<IdentityRole<Guid>> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task OnSubscriptionActivatedAsync(Guid userId, string planCode, DateTime? currentPeriodEndUtc)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return;

        var roleName = RoleTemplates.StoreSeller; 
        await AuthorizationSyncHelper.AssignRoleAndSyncUserClaimsAsync(_userManager, _roleManager, user, roleName);

        
        await UpsertUserClaimAsync(user, "subscription_exp", currentPeriodEndUtc?.ToUniversalTime().ToString("O") ?? string.Empty);
    }

    public async Task OnSubscriptionCanceledAsync(Guid userId, string planCode)
    {
        
        await Task.CompletedTask;
    }

    public async Task OnSubscriptionExpiredAsync(Guid userId, string planCode)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return;

        
        if (await _userManager.IsInRoleAsync(user, RoleTemplates.StoreSeller))
            await _userManager.RemoveFromRoleAsync(user, RoleTemplates.StoreSeller);

        
        if (!await _userManager.IsInRoleAsync(user, RoleTemplates.Seller))
            await _userManager.AddToRoleAsync(user, RoleTemplates.Seller);

        
        await AuthorizationSyncHelper.SyncUserClaimsFromCurrentRolesAsync(_userManager, _roleManager, user);

        
        await UpsertUserClaimAsync(user, "subscription_exp", string.Empty);
    }

    public async Task OnPaymentFailedAsync(Guid userId, string planCode)
    {
       
        await Task.CompletedTask;
    }

    private async Task UpsertUserClaimAsync(AppUser user, string type, string value)
    {
        var claims = await _userManager.GetClaimsAsync(user);
        var existing = claims.FirstOrDefault(c => c.Type == type);
        if (existing is not null)
            await _userManager.RemoveClaimAsync(user, existing);

        if (!string.IsNullOrWhiteSpace(value))
            await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim(type, value));
    }
}
