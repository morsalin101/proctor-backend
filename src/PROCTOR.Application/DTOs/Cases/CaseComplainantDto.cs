namespace PROCTOR.Application.DTOs.Cases;

public class CaseComplainantDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Contact { get; set; }
    public string? AdvisorName { get; set; }
    public string? FatherName { get; set; }
    public string? FatherContact { get; set; }
}

public class CaseAccusedDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AccusedStudentId { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Contact { get; set; }
    public string? GuardianContact { get; set; }
}

public class CreateCaseComplainantRequest
{
    public string Name { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Contact { get; set; }
    public string? AdvisorName { get; set; }
    public string? FatherName { get; set; }
    public string? FatherContact { get; set; }
}

public class CreateCaseAccusedRequest
{
    public string Name { get; set; } = string.Empty;
    public string AccusedStudentId { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Contact { get; set; }
    public string? GuardianContact { get; set; }
}
