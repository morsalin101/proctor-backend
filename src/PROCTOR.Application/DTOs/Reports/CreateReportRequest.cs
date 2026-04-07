namespace PROCTOR.Application.DTOs.Reports;

public class CreateReportRequest
{
    public string Content { get; set; } = string.Empty;
    public bool IsDraft { get; set; } = true;
    public bool IsFinal { get; set; }
    public string? SectionsJson { get; set; }
}
