namespace OnlineShppingSystem.Application.DTOs.AuthDtos;

public record RegisterRequestDto(
    string FullName,
    string Email,
    string PhoneNumber,
    string Password,
    string ConfirmPassword);
