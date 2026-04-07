namespace PROCTOR.Application.DTOs.Cases;

public class CreateCaseRequest
{
    public string StudentName { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

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
    public string? IncidentDate { get; set; }

    // Multiple complainants/accused
    public List<CreateCaseComplainantRequest>? Complainants { get; set; }
    public List<CreateCaseAccusedRequest>? AccusedPersons { get; set; }
}
