using System.ComponentModel.DataAnnotations;

namespace OnlineSohppingSystem.Application.DTOs.Role;

public sealed class UpdateRoleRequestDto
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public List<string>? Permissions { get; set; }
}
