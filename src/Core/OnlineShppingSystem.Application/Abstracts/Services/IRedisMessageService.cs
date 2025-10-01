using OnlineSohppingSystem.Application.DTOs.MessageDto;

namespace OnlineSohppingSystem.Application.Abstracts.Services;

public interface IRedisMessageService
{
    Task<MessageResponse> SendAsync(Guid senderId, SendMessageRequest req, CancellationToken ct = default);
    Task<IReadOnlyList<MessageResponse>> GetConversationAsync(Guid userA, Guid userB, int take = 50,
        DateTimeOffset? before = null, CancellationToken ct = default);
    Task MarkMessagesAsReadAsync(Guid senderId, Guid receiverId);
    Task DeleteMessageForMeAsync(Guid userId, Guid otherUserId, Guid messageId);
    Task DeleteMessageForBothAsync(Guid user1Id, Guid user2Id, Guid messageId);
}
