namespace PROCTOR.Application.DTOs.Reports;

public class CreateReportRequest
{
    public string Content { get; set; } = string.Empty;
    public bool IsDraft { get; set; } = true;
}
