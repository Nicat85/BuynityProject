using FluentValidation;
using OnlineSohppingSystem.Application.DTOs.Notifications;

namespace OnlineSohppingSystem.Application.Validations.NotificationValidations;

public class CreateNotificationRequestValidator : AbstractValidator<CreateNotificationRequest>
{
    public CreateNotificationRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Type).Must(t => string.IsNullOrWhiteSpace(t) ||
                                       new[] { "info", "success", "warning", "error" }.Contains(t!.ToLower()));
    }
}
