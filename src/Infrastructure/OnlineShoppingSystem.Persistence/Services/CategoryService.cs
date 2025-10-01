using AutoMapper;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using OnlineShoppingSystem.Persistence.Contexts;
using OnlineShppingSystem.Application.Abstracts.Repositories;
using OnlineShppingSystem.Application.Abstracts.Services;
using OnlineShppingSystem.Application.DTOs.CategoriesDtos;
using OnlineShppingSystem.Application.Shared;
using OnlineShppingSystem.Domain.Entities;
using System.Net;

namespace OnlineShoppingSystem.Persistence.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepo;
    private readonly IMapper _mapper;

    public CategoryService(ICategoryRepository categoryRepo, IMapper mapper)
    {
        _categoryRepo = categoryRepo;
        _mapper = mapper;

        
    }

    public async Task<BaseResponse<CategoryResultDto>> CreateAsync(CreateCategoryDto dto)
    {
        
        var isExist = await _categoryRepo.AnyAsync(x =>
            x.Name.Trim().ToLower() == dto.Name.Trim().ToLower() &&
            x.ParentId == dto.ParentId);

        if (isExist)
            return BaseResponse<CategoryResultDto>.Fail("Bu adda alt kateqoriya artıq mövcuddur", HttpStatusCode.BadRequest);

        
        var category = _mapper.Map<Category>(dto);

        
        await _categoryRepo.AddAsync(category);
        await _categoryRepo.SaveChangesAsync();

        
        var result = _mapper.Map<CategoryResultDto>(category);

        return BaseResponse<CategoryResultDto>.CreateSuccess(result, "Category created", HttpStatusCode.Created);
    }


    private async Task SoftDeleteRecursiveAsync(Category category)
    {
        category.IsDeleted = true;
        category.DeletedAt = DateTime.UtcNow;

        
        if (category.Children != null && category.Children.Any())
        {
            foreach (var child in category.Children)
            {
                
                var childWithChildren = await _categoryRepo.GetAll(true)
                    .Include(c => c.Children)
                    .FirstOrDefaultAsync(c => c.Id == child.Id);

                if (childWithChildren != null)
                {
                    await SoftDeleteRecursiveAsync(childWithChildren);
                }
            }
        }

        _categoryRepo.Update(category);
    }

    public async Task<BaseResponse<bool>> DeleteAsync(Guid id)
    {
       
        var category = await _categoryRepo.GetAll(true)
            .Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
            return BaseResponse<bool>.Fail("Category not found", HttpStatusCode.NotFound);

        await SoftDeleteRecursiveAsync(category);
        await _categoryRepo.SaveChangesAsync();

        return BaseResponse<bool>.CreateSuccess(true, "Category and its children soft deleted", HttpStatusCode.OK);
    }

    public async Task<BaseResponse<bool>> RestoreAsync(Guid id)
    {
        var category = await _categoryRepo.GetByIdIncludingDeletedAsync(id);
        if (category == null)
            return BaseResponse<bool>.Fail("Category not found", HttpStatusCode.NotFound);

        if (!category.IsDeleted)
            return BaseResponse<bool>.Fail("Category is already active", HttpStatusCode.BadRequest);

        await _categoryRepo.RestoreWithChildrenAsync(category);
        await _categoryRepo.SaveChangesAsync();

        return BaseResponse<bool>.CreateSuccess(true, "Category and children restored", HttpStatusCode.OK);
    }


    public async Task<BaseResponse<List<CategoryResultDto>>> GetAllAsync()
    {
        var categories = await _categoryRepo.GetAllWithChildrenAsync(true); 

        var mainCategories = categories.Where(c => c.ParentId == null).ToList();

        var result = _mapper.Map<List<CategoryResultDto>>(mainCategories);
        return BaseResponse<List<CategoryResultDto>>.CreateSuccess(result);
    }




    public async Task<BaseResponse<CategoryResultDto>> GetByIdAsync(Guid id)
    {
        var category = await _categoryRepo.GetAll(true)
            .Include(x => x.Children)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (category is null)
            return BaseResponse<CategoryResultDto>.Fail("Category not found", HttpStatusCode.NotFound);

        var result = _mapper.Map<CategoryResultDto>(category);
        return BaseResponse<CategoryResultDto>.CreateSuccess(result, "Category found", HttpStatusCode.OK);
    }

    public async Task<BaseResponse<CategoryResultDto>> UpdateAsync(UpdateCategoryDto dto)
    {
        var category = await _categoryRepo.GetByIdAsync(dto.Id);
        if (category is null)
            return BaseResponse<CategoryResultDto>.Fail("Category not found", HttpStatusCode.NotFound);

        category.Name = dto.Name;
        category.ParentId = dto.ParentId;

        _categoryRepo.Update(category);
        await _categoryRepo.SaveChangesAsync();

        var result = _mapper.Map<CategoryResultDto>(category);
        return BaseResponse<CategoryResultDto>.CreateSuccess(result, "Category updated", HttpStatusCode.OK);
    }
}
