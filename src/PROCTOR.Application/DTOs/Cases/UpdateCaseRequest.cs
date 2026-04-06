namespace PROCTOR.Application.DTOs.Cases;

public class UpdateCaseRequest
{
    public string? Priority { get; set; }
    public string? Description { get; set; }
    public string? AssignedToId { get; set; }

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
}
