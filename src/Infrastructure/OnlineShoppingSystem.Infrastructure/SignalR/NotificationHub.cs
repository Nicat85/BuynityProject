using Microsoft.AspNetCore.SignalR;

namespace OnlineShoppingSystem.Infrastructure.SignalR;

public class NotificationHub : Hub
{
    public async Task SendMessage(string receiverUserId, object message)
    {
        await Clients.User(receiverUserId).SendAsync("ReceiveMessage", message);
    }

    public async Task SendNotification(string userId, object notification)
    {
        await Clients.User(userId).SendAsync("ReceiveNotification", notification);
    }
}
