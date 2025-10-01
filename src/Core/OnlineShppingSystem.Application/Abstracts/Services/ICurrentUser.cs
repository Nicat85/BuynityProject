namespace OnlineSohppingSystem.Application.Abstracts.Services;

public interface ICurrentUser
{
    Guid UserId { get; }
    string? Email { get; }
    string? UserName { get; }
    List<string> Roles { get; }
}