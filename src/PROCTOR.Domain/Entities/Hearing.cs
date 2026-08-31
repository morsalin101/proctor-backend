using PROCTOR.Domain.Enums;

namespace PROCTOR.Domain.Entities;

public class Hearing : BaseEntity
{
    public Guid CaseId { get; set; }
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public List<string> Participants { get; set; } = new();
    public HearingStatus Status { get; set; }
    public string? Notes { get; set; }
    public string? Remarks { get; set; }

    /// <summary>The user who set (created) this hearing. Only this user may close it.</summary>
    public Guid? CreatedById { get; set; }
    public string? CreatedByName { get; set; }

    /// <summary>The user who actually conducted (chaired) the hearing. Set when it is completed.</summary>
    public Guid? ConductedById { get; set; }
    public string? ConductedByName { get; set; }
    public DateTime? ConductedAt { get; set; }

    /// <summary>Audit trail of every date/time/location change made after scheduling.</summary>
    public List<HearingReschedule> Reschedules { get; set; } = new();

    /// <summary>Log of external email notifications sent about this hearing.</summary>
    public List<HearingEmailNotification> EmailNotifications { get; set; } = new();

    public Case Case { get; set; } = null!;
}
