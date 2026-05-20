namespace PROCTOR.Application.DTOs.Cases;

public class CaseAssignmentDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public string AssignedAt { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
}

public class AssignCaseRequest
{
    public List<string> UserIds { get; set; } = new();
    public string? PrimaryUserId { get; set; }
}

public class AcknowledgeCaseRequest
{
    public string Comment { get; set; } = string.Empty;
}
