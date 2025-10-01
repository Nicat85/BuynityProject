namespace OnlineSohppingSystem.Application.Events;

public record EmailNotificationEvent(
    Guid UserId,
    string To,
    string Subject,
    string Body,          
    string? FullName = null,
    string? UserName = null,
    string? ProfileImageUrl = null,
    bool UseHtmlTemplate = false
);
