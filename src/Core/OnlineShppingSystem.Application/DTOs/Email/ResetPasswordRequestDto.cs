namespace OnlineShppingSystem.Application.DTOs.EmailDtos;

public record ResetPasswordRequestDto(
    string Email,
    string Token,
    string NewPassword,
    string ConfirmPassword);