using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Permissions;
using PROCTOR.Application.Interfaces;
using PROCTOR.Application.Mapping;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Domain.Interfaces;

namespace PROCTOR.Application.Services;

public class RolePermissionService : IRolePermissionService
{
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<MenuPermission> _menuPermissionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RolePermissionService(
        IRepository<Role> roleRepository,
        IRepository<MenuPermission> menuPermissionRepository,
        IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _menuPermissionRepository = menuPermissionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<List<RoleDto>>> GetAllRolesAsync()
    {
        var roles = await _roleRepository.GetAllAsync();
        var allPermissions = await _menuPermissionRepository.GetAllAsync();
        var permissionsByRole = allPermissions.GroupBy(p => p.RoleId).ToDictionary(g => g.Key, g => g.ToList());

        var dtos = roles.Select(r =>
        {
            r.MenuPermissions = permissionsByRole.GetValueOrDefault(r.Id, new List<MenuPermission>());
            return r.ToDto();
        }).ToList();

        return ApiResponse<List<RoleDto>>.SuccessResponse(dtos);
    }

    public async Task<ApiResponse<RoleDto>> GetRolePermissionsAsync(Guid roleId)
    {
        var role = await _roleRepository.GetByIdAsync(roleId);
        if (role is null)
            return ApiResponse<RoleDto>.FailResponse("Role not found.");

        var permissions = await _menuPermissionRepository.FindAsync(mp => mp.RoleId == roleId);
        role.MenuPermissions = permissions.ToList();

        return ApiResponse<RoleDto>.SuccessResponse(role.ToDto());
    }

    public async Task<ApiResponse<RoleDto>> GetRolePermissionsByNameAsync(string roleName)
    {
        var role = await FindRoleByNameAsync(roleName);
        if (role is null)
            return ApiResponse<RoleDto>.FailResponse("Role not found.");

        var permissions = await _menuPermissionRepository.FindAsync(mp => mp.RoleId == role.Id);
        role.MenuPermissions = permissions.ToList();

        return ApiResponse<RoleDto>.SuccessResponse(role.ToDto());
    }

    public async Task<ApiResponse<RoleDto>> UpdateRolePermissionsAsync(Guid roleId, UpdateMenuPermissionsRequest request)
    {
        var role = await _roleRepository.GetByIdAsync(roleId);
        if (role is null)
            return ApiResponse<RoleDto>.FailResponse("Role not found.");

        var existingPermissions = await _menuPermissionRepository.FindAsync(mp => mp.RoleId == roleId);
        var permissionDict = existingPermissions.ToDictionary(p => p.MenuKey);

        foreach (var permUpdate in request.Permissions)
        {
            if (permissionDict.TryGetValue(permUpdate.MenuKey, out var existing))
            {
                existing.CanCreate = permUpdate.CanCreate;
                existing.CanRead = permUpdate.CanRead;
                existing.CanUpdate = permUpdate.CanUpdate;
                existing.CanDelete = permUpdate.CanDelete;
                existing.UpdatedAt = DateTime.UtcNow;
                _menuPermissionRepository.Update(existing);
            }
            else
            {
                var newPermission = new MenuPermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = roleId,
                    MenuKey = permUpdate.MenuKey,
                    CanCreate = permUpdate.CanCreate,
                    CanRead = permUpdate.CanRead,
                    CanUpdate = permUpdate.CanUpdate,
                    CanDelete = permUpdate.CanDelete
                };
                await _menuPermissionRepository.AddAsync(newPermission);
            }
        }

        await _unitOfWork.SaveChangesAsync();

        // Reload
        var updatedPermissions = await _menuPermissionRepository.FindAsync(mp => mp.RoleId == roleId);
        role.MenuPermissions = updatedPermissions.ToList();

        return ApiResponse<RoleDto>.SuccessResponse(role.ToDto(), "Permissions updated successfully.");
    }

    public async Task<ApiResponse<RoleDto>> UpdateRolePermissionsByNameAsync(string roleName, UpdateMenuPermissionsRequest request)
    {
        var role = await FindRoleByNameAsync(roleName);
        if (role is null)
            return ApiResponse<RoleDto>.FailResponse("Role not found.");

        return await UpdateRolePermissionsAsync(role.Id, request);
    }

    private async Task<Role?> FindRoleByNameAsync(string roleName)
    {
        // Parse the kebab-case role name to the enum value
        if (!TryParseRoleName(roleName, out var roleEnum))
            return null;

        var roles = await _roleRepository.FindAsync(r => r.RoleName == roleEnum);
        return roles.FirstOrDefault();
    }

    private static bool TryParseRoleName(string kebabCaseName, out UserRole result)
    {
        try
        {
            result = MappingExtensions.ParseEnum<UserRole>(kebabCaseName);
            return true;
        }
        catch (ArgumentException)
        {
            result = default;
            return false;
        }
    }
}
