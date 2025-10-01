using OnlineShppingSystem.Application.DTOs.RoleDtos;
using OnlineShppingSystem.Application.Shared;
using OnlineSohppingSystem.Application.DTOs.Role;

public interface IRoleService
{
    Task<BaseResponse<List<RoleDto>>> GetAllRolesAsync();
    Task<BaseResponse<RoleDetailDto>> GetRoleByIdAsync(string roleId);
    Task<BaseResponse<List<string>>> GetAllPermissionsAsync();

    Task<BaseResponse<RoleDto>> CreateRoleWithPermissionsAsync(CreateRoleRequestDto request);
    Task<BaseResponse> UpdateRoleAsync(string roleId, UpdateRoleRequestDto request);
    Task<BaseResponse> DeleteRoleAsync(string roleId);

    Task<BaseResponse> AddPermissionsToRoleAsync(string roleId, List<string> permissions);
    Task<BaseResponse> RemovePermissionsFromRoleAsync(string roleId, List<string> permissions);

    Task<BaseResponse> AssignRoleToUserAsync(AssignRoleToUserRequestDto request);
    Task<BaseResponse> RemoveRoleFromUserAsync(RemoveRoleFromUserRequestDto request);

    Task<BaseResponse<List<RoleDto>>> GetUserRolesAsync(string userId);
    Task<BaseResponse<List<UserInRoleDto>>> GetUsersByRoleIdAsync(string roleId);
}
