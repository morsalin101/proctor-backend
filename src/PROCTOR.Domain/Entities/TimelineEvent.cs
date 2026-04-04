namespace PROCTOR.Domain.Entities;

public class TimelineEvent : BaseEntity
{
    public Guid CaseId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;

    public Case Case { get; set; } = null!;
}
