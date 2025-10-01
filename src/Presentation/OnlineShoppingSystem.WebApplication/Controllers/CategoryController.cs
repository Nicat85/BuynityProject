using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineShppingSystem.Application.Abstracts.Services;
using OnlineShppingSystem.Application.DTOs.CategoriesDtos;
using OnlineShppingSystem.Application.Shared.Helpers;
using OnlineShppingSystem.Application.Shared.Settings;

namespace OnlineShppingSystem.WebApi.Controllers;

[Route("api/categories")]
[ApiController]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _service;

    public CategoryController(ICategoryService service)
    {
        _service = service;
    }


    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var response = await _service.GetAllAsync();
        if (!response.IsSuccess)
            return BadRequest(response.Message);
        return Ok(response);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var response = await _service.GetByIdAsync(id);
        if (!response.IsSuccess)
            return NotFound(response.Message);
        return Ok(response);
    }

   
    [HttpPost]
    [Authorize(Policy = Permissions.Categories.Create)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
    {
        var response = await _service.CreateAsync(dto);
        if (!response.IsSuccess || response.Data == null)
            return BadRequest(response.Message);

        return CreatedAtAction(nameof(GetById), new { id = response.Data.Id }, response);
    }

    [HttpPut]
    [Authorize(Policy = Permissions.Categories.Update)]
    public async Task<IActionResult> Update([FromBody] UpdateCategoryDto dto)
    {
        var response = await _service.UpdateAsync(dto);
        if (!response.IsSuccess)
            return BadRequest(response.Message);
        return Ok(response);
    }

    [HttpPost("{id}/restore")]
    [Authorize(Policy = Permissions.Categories.Restore)]
    public async Task<IActionResult> Restore(Guid id)
    {
        var response = await _service.RestoreAsync(id);
        if (!response.IsSuccess)
            return NotFound(response);

        return Ok(response);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Permissions.Categories.Delete)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var response = await _service.DeleteAsync(id);
        if (!response.IsSuccess)
            return NotFound(response.Message);
        return Ok(response);
    }
}
