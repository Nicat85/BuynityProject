namespace OnlineShppingSystem.Application.DTOs.AuthDtos;

public class AccessTokenResult
{
    public string Token { get; set; } = null!;
    public DateTime Expires { get; set; }
}
