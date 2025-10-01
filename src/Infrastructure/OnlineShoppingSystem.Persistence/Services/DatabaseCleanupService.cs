using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineShoppingSystem.Persistence.Contexts;
using OnlineShppingSystem.Application.Abstracts.Services;
using OnlineShppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Persistence.Services;

public class DatabaseCleanupService : IDatabaseCleanupService
{
    private readonly OnlineShoppingSystemDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<DatabaseCleanupService> _logger;

    public DatabaseCleanupService(
        OnlineShoppingSystemDbContext context,
        UserManager<AppUser> userManager,
        ILogger<DatabaseCleanupService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task CleanExpiredDataAsync()
    {
        var now = DateTime.UtcNow;
        var thresholdDeleted = now.AddDays(-30);
        var thresholdInactive = now.AddDays(-365);

        
        var deletedUsers = await _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.IsDeleted && u.DeletedAt != null && u.DeletedAt < thresholdDeleted)
            .ToListAsync();

        
        var inactiveUsers = await _context.Users
            .IgnoreQueryFilters()
            .Where(u => !u.IsDeleted && u.LastLoginDate != null && u.LastLoginDate < thresholdInactive)
            .ToListAsync();

        var totalUsers = deletedUsers.Concat(inactiveUsers).ToList();

        if (!totalUsers.Any())
        {
            _logger.LogInformation("No users to clean up at {Time}", now);
            return;
        }

        _logger.LogInformation("Cleaning {Count} users at {Time}", totalUsers.Count, now);

        foreach (var user in totalUsers)
        {
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("Deleted user {Email} successfully.", user.Email);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Failed to delete user {Email}. Errors: {Errors}", user.Email, errors);
            }
        }

        _logger.LogInformation("Cleanup job completed at {Time}", now);
    }

}
