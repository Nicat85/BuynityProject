using FluentValidation;
using OnlineShppingSystem.Application.DTOs.EmailDtos;

namespace OnlineShppingSystem.Application.Validations.EmailValidations;

public class ConfirmEmailRequestDtoValidator : AbstractValidator<ConfirmEmailRequestDto>
{
    public ConfirmEmailRequestDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty();
    }
}
