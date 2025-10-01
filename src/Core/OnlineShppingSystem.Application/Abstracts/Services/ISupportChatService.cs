using OnlineSohppingSystem.Application.DTOs.Supports;
using OnlineSohppingSystem.Domain.Enums;

public interface ISupportChatService
{
    Task<Guid> CreateThreadAsync(Guid customerId, string subject);
    Task<SupportMessageDto> SendAsync(Guid threadId, Guid senderId, string text, bool isInternalNote = false, string? attachmentUrl = null);
    Task AssignAsync(Guid threadId, Guid agentId);
    Task SetStatusAsync(Guid threadId, SupportThreadStatus status);
    Task<(IEnumerable<object> messages, int total)> GetMessagesAsync(Guid threadId, Guid requesterId, int page = 1, int pageSize = 50);
    Task<IEnumerable<object>> GetMyThreadsAsync(Guid userId);
    Task<IEnumerable<object>> GetOpenThreadsAsync();
}
