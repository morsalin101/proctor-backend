namespace PROCTOR.Application.DTOs.Hearings;

public class NotifyHearingEmailRequest
{
    public List<string> Recipients { get; set; } = [];
    public string? Subject { get; set; }
    public string Message { get; set; } = string.Empty;
}
