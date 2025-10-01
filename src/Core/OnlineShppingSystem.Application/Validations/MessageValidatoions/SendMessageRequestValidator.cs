using FluentValidation;
using OnlineSohppingSystem.Application.DTOs.MessageDto;

namespace OnlineSohppingSystem.Application.Validations.MessageValidatoions;

public sealed class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(x => x.ReceiverId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
    }
}
