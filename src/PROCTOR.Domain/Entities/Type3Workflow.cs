using PROCTOR.Domain.Enums;

namespace PROCTOR.Domain.Entities;

public class Type3Workflow : BaseEntity
{
    public Guid CaseId { get; set; }
    public Guid ReportId { get; set; }
    public Type3WorkflowStage CurrentStage { get; set; }
    public Guid StartedById { get; set; }
    public string StartedByName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Case Case { get; set; } = null!;
    public Report Report { get; set; } = null!;
    public ICollection<Type3WorkflowTransition> Transitions { get; set; } = new List<Type3WorkflowTransition>();
    public ICollection<DcMemberRemark> MemberRemarks { get; set; } = new List<DcMemberRemark>();
}

public class Type3WorkflowTransition : BaseEntity
{
    public Guid WorkflowId { get; set; }
    public Type3WorkflowStage FromStage { get; set; }
    public Type3WorkflowStage ToStage { get; set; }
    public Guid ActorUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
    public string TargetRole { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public Type3Workflow Workflow { get; set; } = null!;
}

public class DcMemberRemark : BaseEntity
{
    public Guid WorkflowId { get; set; }
    public Guid MemberUserId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string? Content { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? LockedAt { get; set; }
    public Type3Workflow Workflow { get; set; } = null!;
}

public class DisciplinaryResolution : BaseEntity
{
    public string ResolutionNumber { get; set; } = string.Empty;
    public ResolutionStatus Status { get; set; }
    public Guid CreatedById { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public string? ForwardRemarks { get; set; }
    public Guid? ApprovedById { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public ICollection<DisciplinaryResolutionCase> Cases { get; set; } = new List<DisciplinaryResolutionCase>();
}

public class DisciplinaryResolutionCase : BaseEntity
{
    public Guid ResolutionId { get; set; }
    public Guid CaseId { get; set; }
    public int DisplayOrder { get; set; }
    public string ShortDescription { get; set; } = string.Empty;
    public string SecretaryRemarks { get; set; } = string.Empty;
    public DisciplinaryResolution Resolution { get; set; } = null!;
    public Case Case { get; set; } = null!;
}

public class InvestigationAttachment : BaseEntity
{
    public Guid CaseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = "image";
    public string? StorageName { get; set; }
    public string? ExternalUrl { get; set; }
    public string? ContentType { get; set; }
    public long? FileSize { get; set; }
    public Guid UploadedById { get; set; }
    public string UploadedByName { get; set; } = string.Empty;
    public string UploadedByRole { get; set; } = string.Empty;
    public Case Case { get; set; } = null!;
}
