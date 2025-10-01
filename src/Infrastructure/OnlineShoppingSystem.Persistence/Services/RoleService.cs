using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineShppingSystem.Application.Abstracts.Services;
using OnlineShppingSystem.Application.DTOs.RoleDtos;
using OnlineShppingSystem.Application.Shared;
using OnlineShppingSystem.Application.Shared.Settings;
using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Application.DTOs.Role;

public class RoleService : IRoleService
{
    private const string RoleDescriptionClaimType = "RoleDescription";
    private static readonly StringComparer Ci = StringComparer.OrdinalIgnoreCase;

    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILookupNormalizer _normalizer;

    public RoleService(RoleManager<IdentityRole<Guid>> roleManager,
                       UserManager<AppUser> userManager,
                       ILookupNormalizer normalizer)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _normalizer = normalizer;
    }

    private static string CanonicalizeRoleName(string? name)
    {
        var s = (name ?? string.Empty).Trim();
        return Regex.Replace(s, @"\s+", " ");
    }

    private static HashSet<string> AllPermissionsSet()
        => Permissions.GetAll().ToHashSet(Ci);

    public async Task<BaseResponse<List<RoleDto>>> GetAllRolesAsync()
    {
        var roles = await _roleManager.Roles.AsNoTracking().ToListAsync();
        var result = new List<RoleDto>(roles.Count);

        foreach (var role in roles)
        {
            var claims = await _roleManager.GetClaimsAsync(role);
            var permissions = claims.Where(c => c.Type == Permissions.ClaimType)
                                    .Select(c => c.Value)
                                    .Distinct(Ci).ToList();
            var description = claims.FirstOrDefault(c => c.Type == RoleDescriptionClaimType)?.Value;
            var usersCount = (await _userManager.GetUsersInRoleAsync(role.Name!)).Count;

            result.Add(new RoleDto
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                Permissions = permissions,
                Description = description,
                UsersCount = usersCount
            });
        }

        return BaseResponse<List<RoleDto>>.CreateSuccess(result);
    }

    public async Task<BaseResponse<RoleDetailDto>> GetRoleByIdAsync(string roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) return BaseResponse<RoleDetailDto>.Fail("Role not found.", HttpStatusCode.NotFound);

        var claims = await _roleManager.GetClaimsAsync(role);
        var permissions = claims.Where(c => c.Type == Permissions.ClaimType)
                                .Select(c => c.Value).Distinct(Ci).ToList();
        var description = claims.FirstOrDefault(c => c.Type == RoleDescriptionClaimType)?.Value;

        var users = await _userManager.GetUsersInRoleAsync(role.Name!);
        var usersDto = users.Select(u => new UserInRoleDto { Id = u.Id, FullName = u.FullName, Email = u.Email }).ToList();

        return BaseResponse<RoleDetailDto>.CreateSuccess(new RoleDetailDto
        {
            Id = role.Id,
            Name = role.Name!,
            Description = description,
            Permissions = permissions,
            Users = usersDto
        });
    }

    public Task<BaseResponse<List<string>>> GetAllPermissionsAsync()
        => Task.FromResult(BaseResponse<List<string>>.CreateSuccess(Permissions.GetAll()));

    public async Task<BaseResponse<RoleDto>> CreateRoleWithPermissionsAsync(CreateRoleRequestDto request)
    {
        var name = CanonicalizeRoleName(request.Name);
        if (string.IsNullOrWhiteSpace(name))
            return BaseResponse<RoleDto>.Fail("Role name is required.", HttpStatusCode.BadRequest);

        var normalized = _normalizer.NormalizeName(name);
        if (await _roleManager.Roles.AnyAsync(r => r.NormalizedName == normalized))
            return BaseResponse<RoleDto>.Fail("Role already exists.", HttpStatusCode.Conflict);

        var allowed = AllPermissionsSet();
        var uniquePerms = (request.Permissions ?? new()).Distinct(Ci).ToList();
        foreach (var p in uniquePerms)
            if (!allowed.Contains(p))
                return BaseResponse<RoleDto>.Fail($"Invalid permission: {p}", HttpStatusCode.BadRequest);

        var role = new IdentityRole<Guid>(name);
        var created = await _roleManager.CreateAsync(role);
        if (!created.Succeeded)
            return BaseResponse<RoleDto>.Fail(string.Join(", ", created.Errors.Select(e => e.Description)));

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            var addDesc = await _roleManager.AddClaimAsync(role, new Claim(RoleDescriptionClaimType, request.Description));
            if (!addDesc.Succeeded)
                return BaseResponse<RoleDto>.Fail("Failed to add role description.");
        }

        foreach (var permission in uniquePerms)
        {
            var add = await _roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, permission));
            if (!add.Succeeded)
                return BaseResponse<RoleDto>.Fail($"Failed to add permission: {permission}");
        }

        return BaseResponse<RoleDto>.CreateSuccess(new RoleDto
        {
            Id = role.Id,
            Name = role.Name!,
            Permissions = uniquePerms,
            Description = request.Description,
            UsersCount = 0
        }, "Role created successfully.");
    }

    public async Task<BaseResponse> UpdateRoleAsync(string roleId, UpdateRoleRequestDto request)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) return BaseResponse.Fail("Role not found.", HttpStatusCode.NotFound);

        var newName = CanonicalizeRoleName(request.Name);
        if (string.IsNullOrWhiteSpace(newName))
            return BaseResponse.Fail("Role name is required.", HttpStatusCode.BadRequest);

        var normalized = _normalizer.NormalizeName(newName);
        if (await _roleManager.Roles.AnyAsync(r => r.NormalizedName == normalized && r.Id != role.Id))
            return BaseResponse.Fail("Another role with the same name already exists.", HttpStatusCode.Conflict);

        role.Name = newName;
        var upd = await _roleManager.UpdateAsync(role);
        if (!upd.Succeeded)
            return BaseResponse.Fail(string.Join(", ", upd.Errors.Select(e => e.Description)));

      
        var existingClaims = await _roleManager.GetClaimsAsync(role);
        foreach (var c in existingClaims.Where(c => c.Type == Permissions.ClaimType || c.Type == RoleDescriptionClaimType))
        {
            var rm = await _roleManager.RemoveClaimAsync(role, c);
            if (!rm.Succeeded) return BaseResponse.Fail("Failed to clear old claims.");
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            var addDesc = await _roleManager.AddClaimAsync(role, new Claim(RoleDescriptionClaimType, request.Description));
            if (!addDesc.Succeeded) return BaseResponse.Fail("Failed to add role description.");
        }

        var allowed = AllPermissionsSet();
        var uniquePerms = (request.Permissions ?? new()).Distinct(Ci).ToList();
        foreach (var p in uniquePerms)
        {
            if (!allowed.Contains(p))
                return BaseResponse.Fail($"Invalid permission: {p}", HttpStatusCode.BadRequest);

            var add = await _roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, p));
            if (!add.Succeeded) return BaseResponse.Fail($"Failed to add permission: {p}");
        }

        return BaseResponse.Success("Role updated successfully.");
    }

    public async Task<BaseResponse> DeleteRoleAsync(string roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) return BaseResponse.Fail("Role not found.", HttpStatusCode.NotFound);

        var del = await _roleManager.DeleteAsync(role);
        if (!del.Succeeded)
            return BaseResponse.Fail(string.Join(", ", del.Errors.Select(e => e.Description)));

        return BaseResponse.Success("Role deleted successfully.");
    }

    public async Task<BaseResponse> AddPermissionsToRoleAsync(string roleId, List<string> permissions)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) return BaseResponse.Fail("Role not found.", HttpStatusCode.NotFound);

        var allowed = AllPermissionsSet();
        var existing = (await _roleManager.GetClaimsAsync(role))
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value).ToHashSet(Ci);

        foreach (var p in (permissions ?? new()).Distinct(Ci))
        {
            if (!allowed.Contains(p))
                return BaseResponse.Fail($"Invalid permission: {p}", HttpStatusCode.BadRequest);

            if (existing.Contains(p)) continue;

            var res = await _roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, p));
            if (!res.Succeeded) return BaseResponse.Fail($"Failed to add permission: {p}");
        }

        return BaseResponse.Success("Permissions added successfully.");
    }

    public async Task<BaseResponse> RemovePermissionsFromRoleAsync(string roleId, List<string> permissions)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) return BaseResponse.Fail("Role not found.", HttpStatusCode.NotFound);

        var claims = await _roleManager.GetClaimsAsync(role);
        foreach (var p in (permissions ?? new()).Distinct(Ci))
        {
            var claim = claims.FirstOrDefault(c => c.Type == Permissions.ClaimType && Ci.Equals(c.Value, p));
            if (claim == null) continue;

            var res = await _roleManager.RemoveClaimAsync(role, claim);
            if (!res.Succeeded) return BaseResponse.Fail($"Failed to remove permission: {p}");
        }

        return BaseResponse.Success("Permissions removed successfully.");
    }

    public async Task<BaseResponse> AssignRoleToUserAsync(AssignRoleToUserRequestDto request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null) return BaseResponse.Fail("User not found.", HttpStatusCode.NotFound);

        var role = await _roleManager.FindByIdAsync(request.RoleId);
        if (role == null) return BaseResponse.Fail("Role not found.", HttpStatusCode.NotFound);
        if (string.IsNullOrWhiteSpace(role.Name)) return BaseResponse.Fail("Role name is empty.", HttpStatusCode.BadRequest);

       
        var existing = await _userManager.GetRolesAsync(user);
        if (existing.Any())
        {
            var rm = await _userManager.RemoveFromRolesAsync(user, existing);
            if (!rm.Succeeded)
                return BaseResponse.Fail(string.Join(", ", rm.Errors.Select(e => e.Description)));
        }

        
        var add = await _userManager.AddToRoleAsync(user, role.Name);
        if (!add.Succeeded)
            return BaseResponse.Fail(string.Join(", ", add.Errors.Select(e => e.Description)));

        
        var sync = await SyncUserPermissionClaimsWithRolesAsync(user);
        if (!sync.IsSuccess) return sync;

        return BaseResponse.Success("Role assigned (replaced existing) and permissions synchronized.");
    }

    public async Task<BaseResponse> RemoveRoleFromUserAsync(RemoveRoleFromUserRequestDto request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null) return BaseResponse.Fail("User not found.", HttpStatusCode.NotFound);

        var role = await _roleManager.FindByIdAsync(request.RoleId);
        if (role == null) return BaseResponse.Fail("Role not found.", HttpStatusCode.NotFound);
        if (string.IsNullOrWhiteSpace(role.Name)) return BaseResponse.Fail("Role name is empty.", HttpStatusCode.BadRequest);

        if (!await _userManager.IsInRoleAsync(user, role.Name))
            return BaseResponse.Fail("User does not have this role.", HttpStatusCode.Conflict);

        var rem = await _userManager.RemoveFromRoleAsync(user, role.Name);
        if (!rem.Succeeded) return BaseResponse.Fail(string.Join(", ", rem.Errors.Select(e => e.Description)));

        var sync = await SyncUserPermissionClaimsWithRolesAsync(user);
        if (!sync.IsSuccess) return sync;

        return BaseResponse.Success("Role removed and permissions synchronized.");
    }

    public async Task<BaseResponse<List<RoleDto>>> GetUserRolesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return BaseResponse<List<RoleDto>>.Fail("User not found.", HttpStatusCode.NotFound);

        var roleNames = await _userManager.GetRolesAsync(user);
        var list = new List<RoleDto>(roleNames.Count);

        foreach (var roleName in roleNames)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null) continue;

            var claims = await _roleManager.GetClaimsAsync(role);
            var permissions = claims.Where(c => c.Type == Permissions.ClaimType)
                                    .Select(c => c.Value).Distinct(Ci).ToList();
            var description = claims.FirstOrDefault(c => c.Type == RoleDescriptionClaimType)?.Value;

            list.Add(new RoleDto
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                Permissions = permissions,
                Description = description,
                UsersCount = 0
            });
        }

        return BaseResponse<List<RoleDto>>.CreateSuccess(list);
    }

    public async Task<BaseResponse<List<UserInRoleDto>>> GetUsersByRoleIdAsync(string roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) return BaseResponse<List<UserInRoleDto>>.Fail("Role not found.", HttpStatusCode.NotFound);

        var users = await _userManager.GetUsersInRoleAsync(role.Name!);
        var dto = users.Select(u => new UserInRoleDto { Id = u.Id, FullName = u.FullName, Email = u.Email }).ToList();

        return BaseResponse<List<UserInRoleDto>>.CreateSuccess(dto);
    }

   
    private async Task<BaseResponse> SyncUserPermissionClaimsWithRolesAsync(AppUser user)
    {
       
        var currentClaims = await _userManager.GetClaimsAsync(user);
        var toRemove = currentClaims
            .Where(c => c.Type == Permissions.ClaimType || c.Type == ClaimTypes.Role)
            .ToList();

        foreach (var c in toRemove)
        {
            var rm = await _userManager.RemoveClaimAsync(user, c);
            if (!rm.Succeeded)
                return BaseResponse.Fail(string.Join(", ", rm.Errors.Select(e => e.Description)));
        }

        
        var roles = await _userManager.GetRolesAsync(user);
        var allPerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var roleName in roles)
        {
            
            var addRoleClaim = await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, roleName));
            if (!addRoleClaim.Succeeded)
                return BaseResponse.Fail(string.Join(", ", addRoleClaim.Errors.Select(e => e.Description)));

           
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null) continue;

            var roleClaims = await _roleManager.GetClaimsAsync(role);
            foreach (var rc in roleClaims.Where(x => x.Type == Permissions.ClaimType))
                allPerms.Add(rc.Value);
        }

      
        foreach (var p in allPerms)
        {
            var add = await _userManager.AddClaimAsync(user, new Claim(Permissions.ClaimType, p));
            if (!add.Succeeded)
                return BaseResponse.Fail(string.Join(", ", add.Errors.Select(e => e.Description)));
        }

        return BaseResponse.Success();
    }
}
