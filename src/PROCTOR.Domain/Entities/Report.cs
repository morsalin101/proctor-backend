namespace PROCTOR.Domain.Entities;

public class Report : BaseEntity
{
    public Guid CaseId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public Guid CreatedById { get; set; }
    public bool IsDraft { get; set; } = true;
    public bool IsFinal { get; set; }

    public Case Case { get; set; } = null!;
}
