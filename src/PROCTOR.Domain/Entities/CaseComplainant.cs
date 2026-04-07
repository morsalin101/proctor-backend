namespace PROCTOR.Domain.Entities;

public class CaseComplainant : BaseEntity
{
    public Guid CaseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Contact { get; set; }
    public string? AdvisorName { get; set; }
    public string? FatherName { get; set; }
    public string? FatherContact { get; set; }
    public int Order { get; set; }

    public Case Case { get; set; } = null!;
}
