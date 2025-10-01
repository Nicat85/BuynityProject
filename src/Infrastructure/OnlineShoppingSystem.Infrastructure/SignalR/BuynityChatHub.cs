using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OnlineSohppingSystem.Application.Abstracts.Services;
using System.Security.Claims;

namespace OnlineShoppingSystem.Infrastructure.SignalR
{
    [Authorize]
    public class BuynityChatHub : Hub
    {
        private readonly ISupportChatService _svc;

        public BuynityChatHub(ISupportChatService svc)
        {
            _svc = svc;
        }

        private Guid UserId() =>
            Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private static string GroupFor(Guid threadId) => $"thread:{threadId}";

        public async Task JoinThread(Guid threadId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(threadId));
        }

        public async Task SendMessage(Guid threadId, string text, bool isInternalNote = false)
        {
            var userId = UserId();

           
            var msg = await _svc.SendAsync(threadId, userId, text, isInternalNote);

            await Clients.Group(GroupFor(threadId)).SendAsync("messageReceived", msg);
        }

        public async Task Typing(Guid threadId)
        {
            await Clients.Group(GroupFor(threadId)).SendAsync("typing", new { userId = UserId(), threadId });
        }
    }
}
