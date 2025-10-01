namespace OnlineShppingSystem.Application.DTOs.AuthDtos;

public record LoginRequestDto(
    string EmailOrPhoneNumber,
    string Password);
