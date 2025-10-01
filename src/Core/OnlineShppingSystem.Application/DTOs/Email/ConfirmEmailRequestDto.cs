namespace OnlineShppingSystem.Application.DTOs.EmailDtos;

public record ConfirmEmailRequestDto(
    string Email,
    string Token);