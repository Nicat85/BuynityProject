using OnlineShppingSystem.Domain.Entities;

public class SupportChatMessage : BaseEntity
{
    public Guid ThreadId { get; set; }
    public SupportChatThread Thread { get; set; } = null!;

    public Guid? SenderId { get; set; }           
    public AppUser? Sender { get; set; }         

    public string Text { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public bool IsInternalNote { get; set; }
}
