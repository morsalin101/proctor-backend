using PROCTOR.Domain.Enums;

namespace PROCTOR.Domain.Entities;

public class CaseAssignment : BaseEntity
{
    public Guid CaseId { get; set; }
    public Guid UserId { get; set; }
    public Guid? AssignedById { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public bool IsPrimary { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Pending;
    public DateTime? CompletedAt { get; set; }
    public string? CompletionAction { get; set; }

    public Case? Case { get; set; }
    public User? User { get; set; }
}
