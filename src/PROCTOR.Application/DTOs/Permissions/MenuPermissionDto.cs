namespace PROCTOR.Application.DTOs.Permissions;

public class MenuPermissionDto
{
    public string Id { get; set; } = string.Empty;
    public string MenuKey { get; set; } = string.Empty;
    public bool CanCreate { get; set; }
    public bool CanRead { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
}
