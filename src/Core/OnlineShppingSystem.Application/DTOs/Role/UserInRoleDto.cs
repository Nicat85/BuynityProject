namespace OnlineSohppingSystem.Application.DTOs.Role;

public sealed class UserInRoleDto
{
    public Guid Id { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
}