namespace PROCTOR.Domain.Entities;

/// <summary>
/// One entry in a hearing's reschedule history. Stored inline on the Hearing as jsonb so
/// the original slot is never lost when the date/time/location is moved.
/// </summary>
public class HearingReschedule
{
    public string Id { get; set; } = string.Empty;
    public string FromDate { get; set; } = string.Empty;
    public string FromTime { get; set; } = string.Empty;
    public string? FromLocation { get; set; }
    public string ToDate { get; set; } = string.Empty;
    public string ToTime { get; set; } = string.Empty;
    public string? ToLocation { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string RescheduledBy { get; set; } = string.Empty;
    public DateTime RescheduledAt { get; set; }
}
