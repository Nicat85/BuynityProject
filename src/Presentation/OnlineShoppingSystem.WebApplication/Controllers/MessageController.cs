using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OnlineShoppingSystem.Infrastructure.SignalR;
using OnlineShppingSystem.Application.Shared.Helpers;
using OnlineShppingSystem.Application.Shared.Settings;
using OnlineSohppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.DTOs.MessageDto;

namespace OnlineShoppingSystem.WebApplication.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] 
public class MessagesController : ControllerBase
{
    private readonly IRedisMessageService _messages;
    private readonly IHubContext<MessageHub> _hub;
    private readonly ICurrentUser _currentUser;

    public MessagesController(IRedisMessageService messages, IHubContext<MessageHub> hub, ICurrentUser currentUser)
    {
        _messages = messages;
        _hub = hub;
        _currentUser = currentUser;
    }

    
    [HttpPost]
    [Authorize(Policy = Permissions.Messages.Send)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Send([FromBody] SendMessageRequest req, CancellationToken ct)
    {
        var senderId = _currentUser.UserId;
        if (senderId == Guid.Empty) return Unauthorized();
        if (req.ReceiverId == senderId) return BadRequest("Özünə mesaj göndərə bilməzsən.");

        var msg = await _messages.SendAsync(senderId, req, ct);

        await _hub.Clients.User(req.ReceiverId.ToString())
                  .SendAsync("ReceiveMessage", msg, ct);

        return CreatedAtAction(nameof(GetWithUser), new { userId = req.ReceiverId, take = 1 }, msg);
    }

    
    [HttpGet("{userId:guid}")]
    [Authorize(Policy = Permissions.Messages.Read)]
    [ProducesResponseType(typeof(IReadOnlyList<MessageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetWithUser(Guid userId,
                                                 [FromQuery] int take = 50,
                                                 [FromQuery] DateTimeOffset? before = null,
                                                 CancellationToken ct = default)
    {
        var me = _currentUser.UserId;
        if (me == Guid.Empty) return Unauthorized();

        var list = await _messages.GetConversationAsync(me, userId, take, before, ct);
        return Ok(list);
    }

   
    [HttpPost("mark-read/{receiverId:guid}")]
    [Authorize(Policy = Permissions.Messages.MarkRead)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAsRead(Guid receiverId)
    {
        var senderId = _currentUser.UserId;
        if (senderId == Guid.Empty) return Unauthorized();

        await _messages.MarkMessagesAsReadAsync(senderId, receiverId);
        return NoContent();
    }

    
    [HttpDelete("{receiverId:guid}/me/{messageId:guid}")]
    [Authorize(Policy = Permissions.Messages.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> DeleteForMe(Guid receiverId, Guid messageId)
        => DeleteInternal(receiverId, messageId, forBoth: false);

    
    [HttpDelete("{receiverId:guid}/all/{messageId:guid}")]
    [Authorize(Policy = Permissions.Messages.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> DeleteForBoth(Guid receiverId, Guid messageId)
        => DeleteInternal(receiverId, messageId, forBoth: true);

    private async Task<IActionResult> DeleteInternal(Guid receiverId, Guid messageId, bool forBoth)
    {
        var me = _currentUser.UserId;
        if (me == Guid.Empty) return Unauthorized();

        if (forBoth)
            await _messages.DeleteMessageForBothAsync(me, receiverId, messageId);
        else
            await _messages.DeleteMessageForMeAsync(me, receiverId, messageId);

        return NoContent();
    }
}
