using OnlineSohppingSystem.Domain.Enums;

namespace OnlineSohppingSystem.Application.Events;

public class NotificationEvent 
{
    public Guid UserId { get; set; }                
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;

    public string? Link { get; set; }              
    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Low;

    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    public string Source { get; set; } = "app";     
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public IDictionary<string, string>? Metadata { get; set; } 
}
