namespace OnlineSohppingSystem.Application.DTOs.MessageDto;

public sealed class MessageResponse
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public string Content { get; set; } = default!;
    public DateTimeOffset SentAt { get; set; }
    public bool IsRead { get; set; }
    public string? SenderName { get; set; }          
    public string? SenderAvatarUrl { get; set; }
}

