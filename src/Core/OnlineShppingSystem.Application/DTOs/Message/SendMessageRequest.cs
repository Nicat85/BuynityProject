namespace OnlineSohppingSystem.Application.DTOs.MessageDto;

public sealed class SendMessageRequest
{
    public Guid ReceiverId { get; set; }
    public string Content { get; set; } = default!;
    public Guid? ClientMessageId { get; set; }
}
