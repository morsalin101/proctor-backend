namespace PROCTOR.Domain.Entities;

public class MenuPermission : BaseEntity
{
    public Guid RoleId { get; set; }
    public string MenuKey { get; set; } = string.Empty;
    public bool CanCreate { get; set; }
    public bool CanRead { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }

    public Role Role { get; set; } = null!;
}
