namespace PROCTOR.Domain.Entities;

public class Rank : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
}
