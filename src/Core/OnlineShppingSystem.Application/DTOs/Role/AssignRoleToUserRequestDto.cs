using System.ComponentModel.DataAnnotations;

namespace OnlineSohppingSystem.Application.DTOs.Role;

public sealed class AssignRoleToUserRequestDto
{
    public string UserId { get; set; } = default!;
    public string RoleId { get; set; } = default!;
}