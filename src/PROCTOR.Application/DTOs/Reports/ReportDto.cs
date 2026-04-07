namespace PROCTOR.Application.DTOs.Reports;

public class ReportDto
{
    public string Id { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public bool IsDraft { get; set; }
    public bool IsFinal { get; set; }
    public string CreatedDate { get; set; } = string.Empty;
    public string? SectionsJson { get; set; }
}
