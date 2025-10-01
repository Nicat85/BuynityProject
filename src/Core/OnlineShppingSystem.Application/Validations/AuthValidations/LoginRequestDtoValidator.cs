using FluentValidation;
using OnlineShppingSystem.Application.DTOs.AuthDtos;

namespace OnlineShppingSystem.Application.Validations.AuthValidations;

public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(x => x.EmailOrPhoneNumber)
            .NotEmpty().WithMessage("Email, phone number or username is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
