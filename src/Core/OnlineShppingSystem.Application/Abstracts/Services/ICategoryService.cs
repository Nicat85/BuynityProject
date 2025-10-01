using OnlineShppingSystem.Application.DTOs.CategoriesDtos;
using OnlineShppingSystem.Application.Shared;
using OnlineShppingSystem.Domain.Entities;

namespace OnlineShppingSystem.Application.Abstracts.Services;

public interface ICategoryService
{
    Task<BaseResponse<CategoryResultDto>> CreateAsync(CreateCategoryDto dto);
    Task<BaseResponse<CategoryResultDto>> UpdateAsync(UpdateCategoryDto dto);
    Task<BaseResponse<bool>> DeleteAsync(Guid id);
    Task<BaseResponse<List<CategoryResultDto>>> GetAllAsync();
    Task<BaseResponse<CategoryResultDto>> GetByIdAsync(Guid id);
    Task<BaseResponse<bool>> RestoreAsync(Guid id);
}
