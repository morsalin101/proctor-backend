namespace PROCTOR.Application.DTOs.CaseSubjects;

public class CaseSubjectDto
{
    public string Id { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; }
}

public class CreateCaseSubjectRequest
{
    public string Subject { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class UpdateCaseSubjectRequest
{
    public string? Subject { get; set; }
    public int? Order { get; set; }
    public bool? IsActive { get; set; }
}
