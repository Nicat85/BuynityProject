namespace OnlineShppingSystem.Application.DTOs.AuthDtos;

public record UserDto(
    Guid Id,
    string FullName,
    string UserName,
    string Email,
    string PhoneNumber,
    bool EmailConfirmed,
    List<string> Roles,
    List<string> Permissions,
    bool IsDeleted = false,
    string ProfilePicture = "",
    string AvatarText = "",
    string Bio = "",
    string Address = ""
);
