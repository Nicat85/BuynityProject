namespace OnlineSohppingSystem.Application.DTOs.Supports;

public sealed class SupportMessageDto
{
    public Guid Id { get; set; }
    public Guid ThreadId { get; set; }
    public Guid SenderId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public bool IsInternalNote { get; set; }
    public DateTime CreatedAt { get; set; }
}
