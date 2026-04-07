namespace PROCTOR.Domain.Entities;

public class CaseAccused : BaseEntity
{
    public Guid CaseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AccusedStudentId { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Contact { get; set; }
    public string? GuardianContact { get; set; }
    public int Order { get; set; }

    public Case Case { get; set; } = null!;
}
