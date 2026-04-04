using PROCTOR.Domain.Enums;

namespace PROCTOR.Domain.Entities;

public class Role : BaseEntity
{
    public UserRole RoleName { get; set; }
    public string DisplayName { get; set; } = string.Empty;

    public ICollection<MenuPermission> MenuPermissions { get; set; } = new List<MenuPermission>();
}
