using MassTransit;
using Microsoft.AspNetCore.SignalR;
using OnlineShoppingSystem.Infrastructure.SignalR;
using OnlineSohppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.DTOs.Notifications;
using OnlineSohppingSystem.Application.Events;

namespace OnlineShoppingSystem.Persistence.Consumers
{
    public class NotificationConsumer : IConsumer<NotificationEvent>
    {
        private readonly IRedisNotificationService _notificationService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationConsumer(
            IRedisNotificationService notificationService,
            IHubContext<NotificationHub> hubContext)
        {
            _notificationService = notificationService;
            _hubContext = hubContext;
        }

        public async Task Consume(ConsumeContext<NotificationEvent> context)
        {
            var msg = context.Message;


            Console.WriteLine($"[NotificationConsumer] User:{msg.UserId} Title:{msg.Title}");

            var dto = new NotificationDto
            {
                Title = string.IsNullOrWhiteSpace(msg.Title) ? "Bildiriş" : msg.Title,
                Message = msg.Message ?? string.Empty,
                Type = msg.Type.ToString(),
                CreatedAt = msg.CreatedAt,
                Link = msg.Link
            };

            var saved = await _notificationService.CreateNotificationAsync(msg.UserId, dto);

            await _hubContext.Clients.User(msg.UserId.ToString())
                             .SendAsync("ReceiveNotification", saved);
        }
    }
}
