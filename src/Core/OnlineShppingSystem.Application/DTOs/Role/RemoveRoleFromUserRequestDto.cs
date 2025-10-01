using System.ComponentModel.DataAnnotations;

namespace OnlineSohppingSystem.Application.DTOs.Role;

public sealed class RemoveRoleFromUserRequestDto
{
    public string UserId { get; set; } = default!;
    public string RoleId { get; set; } = default!;
}