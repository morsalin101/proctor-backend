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

    public Case Case { get; set; } = null!;
}
