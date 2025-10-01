using Microsoft.AspNetCore.Http;
using OnlineShppingSystem.Application.DTOs.CloudinaryDtos;

namespace OnlineShppingSystem.Application.Abstracts.Services;

public interface ICloudinaryService
{
    Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file);
    Task DeleteImageAsync(string publicId);
}

