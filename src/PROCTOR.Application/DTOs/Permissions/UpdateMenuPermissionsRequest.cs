namespace PROCTOR.Application.DTOs.Permissions;

public class UpdateMenuPermissionsRequest
{
    public List<MenuPermissionUpdateItem> Permissions { get; set; } = [];
}

public class MenuPermissionUpdateItem
{
    public string MenuKey { get; set; } = string.Empty;
    public bool CanCreate { get; set; }
    public bool CanRead { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
}
