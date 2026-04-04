using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Permissions;

namespace PROCTOR.Application.Interfaces;

public interface IRolePermissionService
{
    Task<ApiResponse<List<RoleDto>>> GetAllRolesAsync();
    Task<ApiResponse<RoleDto>> GetRolePermissionsAsync(Guid roleId);
    Task<ApiResponse<RoleDto>> GetRolePermissionsByNameAsync(string roleName);
    Task<ApiResponse<RoleDto>> UpdateRolePermissionsAsync(Guid roleId, UpdateMenuPermissionsRequest request);
    Task<ApiResponse<RoleDto>> UpdateRolePermissionsByNameAsync(string roleName, UpdateMenuPermissionsRequest request);
}
