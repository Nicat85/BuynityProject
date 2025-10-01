using OnlineShppingSystem.Application.DTOs.AuthDtos;
using OnlineShppingSystem.Application.DTOs.TokenDto;
using OnlineShppingSystem.Application.Shared;

namespace OnlineShppingSystem.Application.Abstracts.Services;

public interface IGoogleService
{
    Task<BaseResponse<TokenDto>> GoogleLoginAsync(GoogleAuthRequestDto request);
    Task<BaseResponse<string>> SetPasswordAsync(Guid userId, string newPassword);
}
