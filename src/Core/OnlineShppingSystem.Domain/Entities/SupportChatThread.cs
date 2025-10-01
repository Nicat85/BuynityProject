using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Domain.Entities;
using OnlineSohppingSystem.Domain.Enums;

public class SupportChatThread : BaseEntity
{
    public Guid CustomerId { get; set; }         
    public AppUser? Customer { get; set; }       

    public Guid? AssignedToId { get; set; }      
    public AppUser? AssignedTo { get; set; }     

    public string Subject { get; set; } = string.Empty;
    public SupportThreadStatus Status { get; set; }
    public DateTime LastMessageAt { get; set; }

    public ICollection<SupportChatMessage> Messages { get; set; } = new List<SupportChatMessage>();
}
