namespace PROCTOR.Application.DTOs.Hearings;

public class CreateHearingRequest
{
    public string CaseId { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public List<string> Participants { get; set; } = [];
}
