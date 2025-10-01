using OnlineSohppingSystem.Domain.Enums;

namespace OnlineSohppingSystem.Application.Events;

public record NotificationCreatedEvent(
    Guid UserId,
    string Title,
    string Message,
    NotificationType Type
);

