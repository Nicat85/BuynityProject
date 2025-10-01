using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using OnlineShppingSystem.Application.Abstracts.Services;
using OnlineShppingSystem.Application.Shared;
using OnlineShppingSystem.Domain.Entities;
using System;
using System.Net;
using System.Threading.Tasks;
namespace OnlineShoppingSystem.Persistence.Services;
public class UserProfileService : IProfileService
{
    private readonly Cloudinary _cloudinary;
    private readonly UserManager<AppUser> _userManager;

    public UserProfileService(Cloudinary cloudinary, UserManager<AppUser> userManager)
    {
        _cloudinary = cloudinary;
        _userManager = userManager;
    }

    public async Task<BaseResponse<string>> UploadProfilePictureAsync(Guid userId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BaseResponse<string>.Fail("No file uploaded.");

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return BaseResponse<string>.Fail("User not found.");

        await using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = "user-profiles",
            PublicId = $"profile_{userId}",
            Overwrite = true,
            Transformation = new Transformation().Width(360).Height(360).Crop("fill").Gravity("face")
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.StatusCode != HttpStatusCode.OK && uploadResult.StatusCode != HttpStatusCode.Created)
        {
            return BaseResponse<string>.Fail(uploadResult.Error?.Message ?? "Failed to upload image to Cloudinary.");
        }

        user.ProfilePicture = uploadResult.SecureUrl.ToString();

        if (string.IsNullOrWhiteSpace(user.AvatarText) && !string.IsNullOrWhiteSpace(user.FullName))
        {
            user.AvatarText = GenerateAvatarText(user.FullName);
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BaseResponse<string>.Fail("Failed to update user profile picture.");

        return BaseResponse<string>.CreateSuccess(user.ProfilePicture);
    }

    private string GenerateAvatarText(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return string.Empty;
        if (parts.Length == 1) return parts[0][0].ToString().ToUpper();
        return (parts[0][0].ToString() + parts[1][0].ToString()).ToUpper();
    }
}


