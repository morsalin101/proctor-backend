using PROCTOR.Application.DTOs.Documents;
using PROCTOR.Application.DTOs.Hearings;
using PROCTOR.Application.DTOs.Notes;
using PROCTOR.Application.DTOs.Reports;
using PROCTOR.Application.DTOs.Dashboard;

namespace PROCTOR.Application.DTOs.Cases;

public class CaseDto
{
    public string Id { get; set; } = string.Empty;
    public string CaseNumber { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
    public string? AssignedToId { get; set; }
    public string CreatedDate { get; set; } = string.Empty;
    public string UpdatedDate { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Verdict { get; set; }
    public string? Recommendation { get; set; }
    public string? ForwardedToRole { get; set; }
    public string? SubmittedByUserId { get; set; }

    // Category
    public string? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public bool CategoryIsConfidential { get; set; }

    // Acknowledgment (Type-1)
    public bool IsAcknowledged { get; set; }
    public string? AcknowledgedAt { get; set; }
    public string? AcknowledgedById { get; set; }
    public string? AcknowledgedByName { get; set; }
    public string? AcknowledgmentComment { get; set; }

    // Location (Type-1)
    public double? IncidentLatitude { get; set; }
    public double? IncidentLongitude { get; set; }
    public string? IncidentLocationDescription { get; set; }

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

    public List<CaseAssignmentDto> Assignments { get; set; } = [];
    public List<CaseComplainantDto> Complainants { get; set; } = [];
    public List<CaseAccusedDto> AccusedPersons { get; set; } = [];
    public List<DocumentDto> Documents { get; set; } = [];
    public List<NoteDto> Notes { get; set; } = [];
    public List<HearingDto> Hearings { get; set; } = [];
    public List<RecentActivityDto> Timeline { get; set; } = [];
    public List<ReportDto> Reports { get; set; } = [];
    public List<AdditionalInfoDto> AdditionalInfos { get; set; } = [];
    public List<CaseHearingPersonDto> HearingPersons { get; set; } = [];
}
