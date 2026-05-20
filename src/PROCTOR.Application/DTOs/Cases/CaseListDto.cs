namespace PROCTOR.Application.DTOs.Cases;

public class CaseListDto
{
    public string Id { get; set; } = string.Empty;
    public string CaseNumber { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
    public string CreatedDate { get; set; } = string.Empty;
    public string UpdatedDate { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ForwardedToRole { get; set; }

    public string? CategoryName { get; set; }
    public bool CategoryIsConfidential { get; set; }
    public bool IsAcknowledged { get; set; }

    public string? IncidentLocationDescription { get; set; }
    public double? IncidentLatitude { get; set; }
    public double? IncidentLongitude { get; set; }
    public string? IncidentDate { get; set; }

    public string? StudentDepartment { get; set; }
    public string? StudentContact { get; set; }

    public List<CaseAccusedDto> AccusedPersons { get; set; } = new();
    public List<CaseComplainantDto> Complainants { get; set; } = new();
}
