namespace PROCTOR.Domain.Enums;

public enum Type3WorkflowStage
{
    RegistrarReview,
    VcReview,
    DcChairmanReview,
    DcMemberReview,
    DcSecretaryReview,
    ResolutionDcChairmanReview,
    ChairmanReview,
    Completed
}

public enum ResolutionStatus
{
    Draft,
    PendingDcChairman,
    PendingChairman,
    Approved
}
