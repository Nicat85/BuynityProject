namespace OnlineShppingSystem.Application.DTOs.AuthDtos;

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime Expiration);
