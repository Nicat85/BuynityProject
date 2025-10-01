using Microsoft.AspNetCore.Http;
using OnlineShppingSystem.Application.Shared;

namespace OnlineShppingSystem.Application.Abstracts.Services;

public interface IProfileService
{
    Task<BaseResponse<string>> UploadProfilePictureAsync(Guid userId, IFormFile file);
}
