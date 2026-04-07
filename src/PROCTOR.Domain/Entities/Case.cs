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
    public string? Verdict { get; set; }
    public string? Recommendation { get; set; }
    public string? ForwardedToRole { get; set; }
    public Guid? SubmittedByUserId { get; set; }

    // Type-2 form fields
    public string? StudentDepartment { get; set; }
    public string? StudentContact { get; set; }
    public string? StudentAdvisorName { get; set; }
    public string? StudentFatherName { get; set; }
    public string? StudentFatherContact { get; set; }
    public string? AccusedName { get; set; }
    public string? AccusedId { get; set; }
    public string? AccusedDepartment { get; set; }
    public string? AccusedContact { get; set; }
    public string? AccusedGuardianContact { get; set; }
    public string? VideoLink { get; set; }
    public DateTime? IncidentDate { get; set; }

    public User? AssignedTo { get; set; }
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<Note> Notes { get; set; } = new List<Note>();
    public ICollection<Hearing> Hearings { get; set; } = new List<Hearing>();
    public ICollection<TimelineEvent> TimelineEvents { get; set; } = new List<TimelineEvent>();
    public ICollection<Report> Reports { get; set; } = new List<Report>();
    public ICollection<CaseVerification> Verifications { get; set; } = new List<CaseVerification>();
    public ICollection<CaseComplainant> Complainants { get; set; } = new List<CaseComplainant>();
    public ICollection<CaseAccused> AccusedPersons { get; set; } = new List<CaseAccused>();
}
