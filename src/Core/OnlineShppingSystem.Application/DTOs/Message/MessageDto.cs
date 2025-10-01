namespace OnlineSohppingSystem.Application.DTOs.MessageDto;

public class MessageDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public string Content { get; set; } = null!;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
    public List<Guid> DeletedForUserIds { get; set; } = new();
}
