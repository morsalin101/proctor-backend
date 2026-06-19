namespace PROCTOR.Domain.Entities;

/// <summary>
/// A person added to a case's hearing panel. Either an internal system user or an
/// external person who is only notified by email. Stored inline on the Case as jsonb.
/// </summary>
public class CaseHearingPerson
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "internal"; // "internal" | "external"
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? UserId { get; set; }
    public string? Role { get; set; }
    public DateTime AddedAt { get; set; }
}
