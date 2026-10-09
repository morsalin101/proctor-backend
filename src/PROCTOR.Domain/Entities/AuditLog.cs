namespace PROCTOR.Domain.Entities;

/// <summary>
/// Immutable record of an API action. Audit logs are written by middleware after a request
/// finishes and are never exposed through create, update, or delete endpoints.
/// </summary>
public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = "Anonymous";
    public string UserRole { get; set; } = "anonymous";
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string HttpMethod { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? QueryString { get; set; }
    public int StatusCode { get; set; }
    public bool Succeeded { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public long DurationMs { get; set; }
    public string? DetailsJson { get; set; }
}
