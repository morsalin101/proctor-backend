namespace PROCTOR.Application.DTOs.Students;

public class StudentDto
{
    public string Id { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Contact { get; set; }
    public string? Email { get; set; }
    public string Gender { get; set; } = "unspecified";
    public string? AdvisorName { get; set; }
    public string? FatherName { get; set; }
    public string? FatherContact { get; set; }
    public string? GuardianContact { get; set; }
    public bool IsActive { get; set; }
}

public class CreateStudentRequest
{
    public string StudentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Contact { get; set; }
    public string? Email { get; set; }
    public string? Gender { get; set; }
    public string? AdvisorName { get; set; }
    public string? FatherName { get; set; }
    public string? FatherContact { get; set; }
    public string? GuardianContact { get; set; }
}
