using OnlineSohppingSystem.Application.DTOs.Notifications;

namespace OnlineSohppingSystem.Application.Abstracts.Services
{
    public interface IRedisNotificationService
    {
        Task<NotificationDto> CreateNotificationAsync(Guid userId, NotificationDto dto, CancellationToken ct = default);

        Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId, CancellationToken ct = default);

        
        Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default);

        
        Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);

      
        Task<bool> DeleteAsync(Guid userId, Guid notificationId, CancellationToken ct = default);

        
        Task<int> DeleteAllAsync(Guid userId, CancellationToken ct = default);
    }
}
