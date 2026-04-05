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
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
    public string CreatedDate { get; set; } = string.Empty;
    public string UpdatedDate { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Verdict { get; set; }
    public string? Recommendation { get; set; }
    public string? ForwardedToRole { get; set; }
    public List<DocumentDto> Documents { get; set; } = [];
    public List<NoteDto> Notes { get; set; } = [];
    public List<HearingDto> Hearings { get; set; } = [];
    public List<RecentActivityDto> Timeline { get; set; } = [];
    public List<ReportDto> Reports { get; set; } = [];
}
