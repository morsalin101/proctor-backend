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

    /// <summary>Log of external email notifications sent about this hearing.</summary>
    public List<HearingEmailNotification> EmailNotifications { get; set; } = new();

    public Case Case { get; set; } = null!;
}
