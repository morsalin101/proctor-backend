namespace PROCTOR.Application.DTOs.AuditLogs;

public class AuditLogDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
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
    public DateTime CreatedAt { get; set; }
}

public class AuditLogFiltersDto
{
    public List<string> Actions { get; set; } = [];
    public List<string> Roles { get; set; } = [];
    public List<string> EntityTypes { get; set; } = [];
}
