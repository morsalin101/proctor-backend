namespace PROCTOR.Domain.Entities;

public class CaseVerification : BaseEntity
{
    public Guid CaseId { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string ChecklistResultsJson { get; set; } = "[]";
    public string CreatedByName { get; set; } = string.Empty;

    public Case Case { get; set; } = null!;
}
