namespace OnlineSohppingSystem.Application.DTOs.Message;

public sealed class SendMessageDto
{
    public string Text { get; set; } = string.Empty;
    public bool IsInternalNote { get; set; } = false;
    public string? AttachmentUrl { get; set; }
}
