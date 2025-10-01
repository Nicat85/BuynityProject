using FluentValidation;
using OnlineShppingSystem.Application.DTOs.RoleDtos;
using OnlineSohppingSystem.Application.DTOs.Role;

namespace OnlineShppingSystem.Application.Validations.RoleValidations;

public class RemoveRoleFromUserRequestDtoValidator : AbstractValidator<RemoveRoleFromUserRequestDto>
{
    public RemoveRoleFromUserRequestDtoValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RoleId).NotEmpty();
    }
}
