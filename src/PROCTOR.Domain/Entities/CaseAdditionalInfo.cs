namespace PROCTOR.Domain.Entities;

/// <summary>
/// A free-text "additional information" entry added to a case by an associated staff
/// member (coordinator, proctor, etc.) after the case was submitted. Shown in the
/// case Overview tab.
/// </summary>
public class CaseAdditionalInfo : BaseEntity
{
    public Guid CaseId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? AuthorRole { get; set; }

    public Case Case { get; set; } = null!;
}
