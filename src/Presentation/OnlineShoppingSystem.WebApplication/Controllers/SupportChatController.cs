using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineSohppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.DTOs.Supports;
using OnlineSohppingSystem.Domain.Enums;
using OnlineShppingSystem.Application.Shared.Settings;
using System.Security.Claims;
using System.Net;
using OnlineSohppingSystem.Application.DTOs.Message;

namespace OnlineShoppingSystem.WebApplication.Controllers
{
    [ApiController]
    [Route("api/support/chat")]
    [Produces("application/json")]
    public sealed class SupportChatController : ControllerBase
    {
        private readonly ISupportChatService _svc;
        public SupportChatController(ISupportChatService svc) => _svc = svc;

        private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

       
        [Authorize]
        [HttpPost("threads")]
        public async Task<ActionResult<object>> CreateThread([FromBody] CreateThreadDto dto)
        {
            var id = await _svc.CreateThreadAsync(UserId(), dto.Subject ?? "Support");
            return Ok(new { threadId = id });
        }

        
        [Authorize]
        [HttpGet("threads/mine")]
        public async Task<ActionResult<IEnumerable<object>>> MyThreads()
        {
            var res = await _svc.GetMyThreadsAsync(UserId());
            return Ok(res);
        }

        
        [Authorize(Policy = Permissions.SupportChat.Read)]
        [HttpGet("threads/open")]
        public async Task<ActionResult<IEnumerable<object>>> OpenThreads()
        {
            var res = await _svc.GetOpenThreadsAsync();
            return Ok(res);
        }

        
        [Authorize]
        [HttpGet("threads/{threadId:guid}/messages")]
        public async Task<ActionResult<object>> Messages(Guid threadId, int page = 1, int pageSize = 50)
        {
            var (messages, total) = await _svc.GetMessagesAsync(threadId, UserId(), page, pageSize);
            return Ok(new { total, messages });
        }

        
        [Authorize]
        [HttpPost("threads/{threadId:guid}/messages")]
        public async Task<IActionResult> Send(Guid threadId, [FromBody] SendMessageDto dto)
        {
            if (dto.IsInternalNote && !User.IsInRole("Moderator") && !User.IsInRole("SupportAgent"))
                return StatusCode((int)HttpStatusCode.Forbidden, new { message = "Only agents can add internal notes." });

            var msg = await _svc.SendAsync(threadId, UserId(), dto.Text, dto.IsInternalNote, dto.AttachmentUrl);
            return Ok(msg);
        }

        
        [Authorize(Policy = Permissions.SupportChat.Assign)]
        [HttpPost("threads/{threadId:guid}/assign/{agentId:guid}")]
        public async Task<IActionResult> Assign(Guid threadId, Guid agentId)
        {
            await _svc.AssignAsync(threadId, agentId);
            return Ok();
        }

        
        [Authorize(Policy = Permissions.SupportChat.ChangeStatus)]
        [HttpPost("threads/{threadId:guid}/status/{status}")]
        public async Task<IActionResult> SetStatus(Guid threadId, SupportThreadStatus status)
        {
            await _svc.SetStatusAsync(threadId, status);
            return Ok();
        }

       
        [Authorize(Policy = Permissions.SupportChat.Close)]
        [HttpPost("threads/{threadId:guid}/status/close")]
        public async Task<IActionResult> CloseThread(Guid threadId)
        {
            await _svc.SetStatusAsync(threadId, SupportThreadStatus.Closed);
            return Ok();
        }
    }
}
