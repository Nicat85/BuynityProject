namespace OnlineSohppingSystem.Application.DTOs.Role;

public sealed class RoleDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public List<string> Permissions { get; set; } = new();
    public List<UserInRoleDto> Users { get; set; } = new();
}