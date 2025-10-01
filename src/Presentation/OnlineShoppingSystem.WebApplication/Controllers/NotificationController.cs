using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OnlineShoppingSystem.Infrastructure.SignalR;
using OnlineShppingSystem.Application.Common.Extensions;
using OnlineShppingSystem.Application.Shared.Helpers;
using OnlineShppingSystem.Application.Shared.Settings;
using OnlineSohppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.DTOs.Notifications;

namespace OnlineShoppingSystem.WebApplication.Controllers
{
    [Route("api/notifications")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly IRedisNotificationService _notificationService;
        private readonly IHubContext<NotificationHub> _hub;

        public NotificationController(IRedisNotificationService notificationService, IHubContext<NotificationHub> hub)
        {
            _notificationService = notificationService;
            _hub = hub;
        }

        [HttpGet]
        [Authorize(Policy = Permissions.Notifications.Read)]
        public async Task<IActionResult> Get(CancellationToken ct)
        {
            var userId = User.GetUserId();
            var notifs = await _notificationService.GetUserNotificationsAsync(userId, ct);
            return Ok(notifs);
        }

        [HttpPost("mark-as-read/{id:guid}")]
        [Authorize(Policy = Permissions.Notifications.MarkRead)]
        public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var ok = await _notificationService.MarkAsReadAsync(userId, id, ct);
            return ok ? NoContent() : NotFound();
        }

        [HttpPost("push")]
        [Authorize(Policy = Permissions.Notifications.Create)]
        public async Task<IActionResult> PushNotification([FromBody] CreateNotificationRequest req, CancellationToken ct)
        {
            var userId = User.GetUserId();

            var incoming = new NotificationDto
            {
                Title = req.Title,
                Message = req.Message,
                Type = req.Type,
                Link = req.Link
            };

            var saved = await _notificationService.CreateNotificationAsync(userId, incoming, ct);

            await _hub.Clients.User(userId.ToString())
                      .SendAsync("ReceiveNotification", saved, ct);

            return Ok(saved);
        }

        [HttpPost("mark-all-as-read")]
        [Authorize(Policy = Permissions.Notifications.MarkAllRead)]
        public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
        {
            var userId = User.GetUserId();
            var changed = await _notificationService.MarkAllAsReadAsync(userId, ct);
            
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = Permissions.Notifications.Delete)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var ok = await _notificationService.DeleteAsync(userId, id, ct);
            return ok ? NoContent() : NotFound();
        }

        [HttpDelete("all")]
        [Authorize(Policy = Permissions.Notifications.DeleteAll)]
        public async Task<IActionResult> DeleteAll(CancellationToken ct)
        {
            var userId = User.GetUserId();
            var count = await _notificationService.DeleteAllAsync(userId, ct);
            return NoContent(); 
        }
    }
}
