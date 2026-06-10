namespace PROCTOR.Domain.Entities;

/// <summary>
/// A record of an email notification sent about a hearing to external recipients
/// (people who are not system users). Stored inline on the Hearing as jsonb.
/// </summary>
public class HearingEmailNotification
{
    public List<string> Recipients { get; set; } = new();
    public string? Subject { get; set; }
    public string Message { get; set; } = string.Empty;
    public string SentBy { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}
