using OnlineShppingSystem.Application.DTOs.AuthDtos;
using OnlineShppingSystem.Application.DTOs.TokenDto;
using OnlineShppingSystem.Application.Shared;

namespace OnlineShppingSystem.Application.Abstracts.Services;

public interface IFacebookService
{
    Task<BaseResponse<TokenDto>> LoginWithFacebookAsync(string accessToken);
}
