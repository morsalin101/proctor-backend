namespace PROCTOR.Application.DTOs.Hearings;

public class HearingDto
{
    public string Id { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public List<string> Participants { get; set; } = [];
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? Remarks { get; set; }
}
