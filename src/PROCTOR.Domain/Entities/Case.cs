using PROCTOR.Domain.Enums;

namespace PROCTOR.Domain.Entities;

public class Case : BaseEntity
{
    public string CaseNumber { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public CaseType Type { get; set; }
    public CaseStatus Status { get; set; }
    public Priority Priority { get; set; }
    public Guid? AssignedToId { get; set; }
    public string Description { get; set; } = string.Empty;

    public User? AssignedTo { get; set; }
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<Note> Notes { get; set; } = new List<Note>();
    public ICollection<Hearing> Hearings { get; set; } = new List<Hearing>();
    public ICollection<TimelineEvent> TimelineEvents { get; set; } = new List<TimelineEvent>();
}
