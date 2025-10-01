using FluentValidation;
using OnlineShppingSystem.Application.Shared.Settings;   
using OnlineSohppingSystem.Application.DTOs.Role;

namespace OnlineShppingSystem.Application.Validations.RoleValidations;

public class CreateRoleRequestDtoValidator : AbstractValidator<CreateRoleRequestDto>
{
    public CreateRoleRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(3);

        RuleForEach(x => x.Permissions)
            .Must(p => Permissions.Exists(p)) 
            .WithMessage(p => $"Invalid permission: {p}");
    }
}
