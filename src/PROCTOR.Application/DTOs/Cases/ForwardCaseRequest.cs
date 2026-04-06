namespace PROCTOR.Application.DTOs.Cases;

public class ForwardCaseRequest
{
    public string TargetRole { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? Recommendation { get; set; }
    public string? Verdict { get; set; }
    public string? AssignedToUserId { get; set; }
    public bool ForwardToAll { get; set; }
}
