namespace PROCTOR.Application.DTOs.Hearings;

public class HearingDto
{
    public string Id { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string? CaseNumber { get; set; }
    public string? StudentName { get; set; }
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public List<string> Participants { get; set; } = [];
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? Remarks { get; set; }
    public List<HearingEmailNotificationDto> EmailNotifications { get; set; } = [];
}

public class HearingEmailNotificationDto
{
    public List<string> Recipients { get; set; } = [];
    public string? Subject { get; set; }
    public string Message { get; set; } = string.Empty;
    public string SentBy { get; set; } = string.Empty;
    public string SentAt { get; set; } = string.Empty;
}

public class UpcomingHearingsDto
{
    public List<HearingDto> Today { get; set; } = new();
    public List<HearingDto> Tomorrow { get; set; } = new();
    public List<HearingDto> ThisWeek { get; set; } = new();
    public List<HearingDto> Later { get; set; } = new();
}
