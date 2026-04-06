using PROCTOR.Application.Interfaces;
using PROCTOR.Application.Mapping;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Domain.Interfaces;

namespace PROCTOR.Application.Services;

public class PermissionChecker : IPermissionChecker
{
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<MenuPermission> _menuPermissionRepository;

    public PermissionChecker(
        IRepository<Role> roleRepository,
        IRepository<MenuPermission> menuPermissionRepository)
    {
        _roleRepository = roleRepository;
        _menuPermissionRepository = menuPermissionRepository;
    }

    public async Task<bool> HasPermissionAsync(string userRole, string menuKey, string operation)
    {
        if (userRole == "super-admin")
            return true;

        UserRole roleEnum;
        try
        {
            roleEnum = MappingExtensions.ParseEnum<UserRole>(userRole);
        }
        catch (ArgumentException)
        {
            return false;
        }

        var roles = await _roleRepository.FindAsync(r => r.RoleName == roleEnum);
        var role = roles.FirstOrDefault();
        if (role is null)
            return false;

        var permissions = await _menuPermissionRepository.FindAsync(
            mp => mp.RoleId == role.Id && mp.MenuKey == menuKey);
        var permission = permissions.FirstOrDefault();
        if (permission is null)
            return false;

        return operation.ToLower() switch
        {
            "create" => permission.CanCreate,
            "read" => permission.CanRead,
            "update" => permission.CanUpdate,
            "delete" => permission.CanDelete,
            _ => false
        };
    }
}
