using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineShppingSystem.Application.Abstracts.Services;
using OnlineShppingSystem.Application.DTOs.RoleDtos;
using OnlineShppingSystem.Application.Shared.Settings;
using OnlineSohppingSystem.Application.DTOs.Role;

[ApiController]
[Route("api/[controller]")]
public sealed class RoleController : ControllerBase
{
    private readonly IRoleService _roleService;
    public RoleController(IRoleService roleService) => _roleService = roleService;

    [HttpGet]
    [Authorize(Policy = Permissions.Roles.Read)]
    public async Task<IActionResult> GetAll()
        => Ok(await _roleService.GetAllRolesAsync());

    [HttpGet("{id}")]
    [Authorize(Policy = Permissions.Roles.Read)]
    public async Task<IActionResult> GetById(string id)
        => Ok(await _roleService.GetRoleByIdAsync(id));

    [HttpPost]
    [Authorize(Policy = Permissions.Roles.Create)]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequestDto dto)
        => Ok(await _roleService.CreateRoleWithPermissionsAsync(dto));

    [HttpPut("{id}")]
    [Authorize(Policy = Permissions.Roles.Update)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateRoleRequestDto dto)
        => Ok(await _roleService.UpdateRoleAsync(id, dto));

    [HttpDelete("{id}")]
    [Authorize(Policy = Permissions.Roles.Delete)]
    public async Task<IActionResult> Delete(string id)
        => Ok(await _roleService.DeleteRoleAsync(id));

    [HttpPost("{roleId}/permissions")]
    [Authorize(Policy = Permissions.Roles.UpdatePermissions)]
    public async Task<IActionResult> AddPerms(string roleId, [FromBody] List<string> permissions)
        => Ok(await _roleService.AddPermissionsToRoleAsync(roleId, permissions));

    [HttpDelete("{roleId}/permissions")]
    [Authorize(Policy = Permissions.Roles.UpdatePermissions)]
    public async Task<IActionResult> RemovePerms(string roleId, [FromBody] List<string> permissions)
        => Ok(await _roleService.RemovePermissionsFromRoleAsync(roleId, permissions));

    
    [HttpPost("assign")]
    [Authorize(Policy = Permissions.Roles.AssignToUser)]
    public async Task<IActionResult> Assign([FromBody] AssignRoleToUserRequestDto dto)
        => Ok(await _roleService.AssignRoleToUserAsync(dto));
}
