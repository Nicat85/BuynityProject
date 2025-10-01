namespace OnlineSohppingSystem.Application.DTOs.Notifications;

public class CreateNotificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Link { get; set; }
}
