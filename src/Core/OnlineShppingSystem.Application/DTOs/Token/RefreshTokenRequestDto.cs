using System.ComponentModel.DataAnnotations;

namespace OnlineShppingSystem.Application.DTOs.TokenDto;

public class RefreshTokenRequestDto
{
    public string RefreshToken { get; set; } = null!;
}