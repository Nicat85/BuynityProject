namespace OnlineShppingSystem.Application.Abstracts.Services;

public interface IDatabaseCleanupService
{
    Task CleanExpiredDataAsync();
}
