public class NotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Type { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? Link { get; set; }

    
    public Guid? SenderId { get; set; }         
    public string? SenderName { get; set; }      
    public string? SenderAvatarUrl { get; set; }
}
