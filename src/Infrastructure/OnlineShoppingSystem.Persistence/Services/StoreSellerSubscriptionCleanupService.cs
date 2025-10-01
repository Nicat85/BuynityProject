using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineShoppingSystem.Persistence.Contexts;
using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Domain.Enums;
using OnlineShppingSystem.Application.Shared.Helpers;

namespace OnlineShoppingSystem.Persistence.Services;

public class StoreSellerSubscriptionCleanupService : IStoreSellerSubscriptionCleanupService
{
    private const string StoreSellerRole = "StoreSeller";
    private const string SellerRole = "Seller";

    private readonly OnlineShoppingSystemDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public StoreSellerSubscriptionCleanupService(
        OnlineShoppingSystemDbContext db,
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task CleanExpiredSubscriptionsAsync()
    {
        var now = DateTime.UtcNow;

        var expiredSubs = await _db.StoreSellerSubscriptions
            .Include(s => s.User)
            .Where(s => s.Status == SubscriptionStatus.Active &&
                        s.CurrentPeriodEnd != null &&
                        s.CurrentPeriodEnd < now)
            .ToListAsync();

        foreach (var sub in expiredSubs)
        {
            sub.Status = SubscriptionStatus.Canceled;

            var user = sub.User;
            if (user != null)
            {
                if (await _userManager.IsInRoleAsync(user, StoreSellerRole))
                    await _userManager.RemoveFromRoleAsync(user, StoreSellerRole);

                if (!await _userManager.IsInRoleAsync(user, SellerRole))
                    await _userManager.AddToRoleAsync(user, SellerRole);

               
                await AuthorizationSyncHelper.SyncUserClaimsFromCurrentRolesAsync(_userManager, _roleManager, user);
            }
        }

        await _db.SaveChangesAsync();
    }
}
