using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using OnlineSohppingSystem.Application.Shared.Settings;

namespace OnlineShppingSystem.Application.Shared.Helpers;

public static class DataSeeder
{
    public static async Task SeedAsync(RoleManager<IdentityRole<Guid>> roleManager,
                                       IOptions<IdentitySeedSettings> seedOptions)
    {
        var opt = seedOptions.Value ?? new IdentitySeedSettings();

        if (!opt.Enabled) return;

        if (opt.SeedRoles && opt.Roles is { Length: > 0 })
        {
            foreach (var roleName in opt.Roles.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (await roleManager.FindByNameAsync(roleName) is null)
                {
                    await roleManager.CreateAsync(new IdentityRole<Guid>
                    {
                        Name = roleName,
                        NormalizedName = roleName.ToUpperInvariant()
                    });
                }
            }
        }
    }
}
