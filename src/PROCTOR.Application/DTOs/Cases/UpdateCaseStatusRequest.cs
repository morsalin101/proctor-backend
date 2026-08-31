namespace PROCTOR.Application.DTOs.Cases;

public class UpdateCaseStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? Verdict { get; set; }
    public string? Recommendation { get; set; }

    /// <summary>Mandatory when moving a case to Closed — the reason the case is being closed.</summary>
    public string? ClosingMessage { get; set; }
}
