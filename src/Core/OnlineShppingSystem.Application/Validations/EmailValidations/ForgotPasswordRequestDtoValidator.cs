using FluentValidation;
using OnlineShppingSystem.Application.DTOs.EmailDtos;

namespace OnlineShppingSystem.Application.Validations.EmailValidations;

public class ForgotPasswordRequestDtoValidator : AbstractValidator<ForgotPasswordRequestDto>
{
    public ForgotPasswordRequestDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
