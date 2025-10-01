using OnlineShppingSystem.Application.DTOs.AuthDtos;
using OnlineShppingSystem.Application.DTOs.TokenDto;
using OnlineShppingSystem.Domain.Entities;

namespace OnlineShppingSystem.Application.Abstracts.Services;

public interface ITokenService
{
    Task<TokenDto> CreateTokenAsync(AppUser user);
    Task<string> GenerateAccessTokenAsync(AppUser user);
}


