namespace PROCTOR.Domain.Entities;

public class VerificationChecklistItem : BaseEntity
{
    public string Label { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
}
