namespace PROCTOR.Application.DTOs.Permissions;

public class RoleDto
{
    public string Id { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<MenuPermissionDto> MenuPermissions { get; set; } = [];
}
