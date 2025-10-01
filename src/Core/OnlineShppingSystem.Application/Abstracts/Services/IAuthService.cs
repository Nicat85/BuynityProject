using Microsoft.AspNetCore.Http;
using OnlineShppingSystem.Application.DTOs.AuthDtos;
using OnlineShppingSystem.Application.DTOs.EmailDtos;
using OnlineShppingSystem.Application.DTOs.TokenDto;
using OnlineShppingSystem.Application.Shared;

namespace OnlineShppingSystem.Application.Abstracts.Services;

public interface IAuthService
{
    Task<BaseResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto request, IFormFile? profilePictureFile = null);
    Task<BaseResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request);
    Task<BaseResponse<TokenDto>> RefreshTokenAsync(string refreshToken, string oldAccessToken);
    Task<BaseResponse<List<UserDto>>> GetAllUsersAsync();
    Task<BaseResponse<UserDto>> GetProfileAsync(Guid userId);
    Task<BaseResponse<string>> ConfirmEmailAsync(ConfirmEmailRequestDto request);
    Task<BaseResponse<string>> ForgotPasswordAsync(ForgotPasswordRequestDto request);
    Task<BaseResponse<string>> ResetPasswordAsync(ResetPasswordRequestDto request);
    Task<BaseResponse<UserDto>> EditProfileAsync(Guid userId, EditProfileDto request);
    Task<BaseResponse<string>> DeleteAccountAsync(Guid userId);
    Task<BaseResponse<string>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
    Task<BaseResponse<string>> ResendEmailConfirmationAsync(string email);
    Task<BaseResponse> LogoutAsync(string refreshToken, string accessToken);

}