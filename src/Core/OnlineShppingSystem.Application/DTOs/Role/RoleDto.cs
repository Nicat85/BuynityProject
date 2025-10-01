namespace OnlineShppingSystem.Application.DTOs.RoleDtos;

public sealed class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public List<string> Permissions { get; set; } = new();
    public string? Description { get; set; }
    public int UsersCount { get; set; }
}