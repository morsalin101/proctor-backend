namespace PROCTOR.Application.DTOs.Hearings;

public class UpdateHearingRequest
{
    public string? Date { get; set; }
    public string? Time { get; set; }
    public string? Location { get; set; }
    public List<string>? Participants { get; set; }
    public string? Notes { get; set; }
    public string? Remarks { get; set; }
}
