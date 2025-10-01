
namespace OnlineShppingSystem.Application.Abstracts.Services;

public interface IProductCleanupService
{
    Task HardDeleteOldSoftDeletedProductsAsync();
    Task SendPreDeletionReminderAsync();
}
