namespace PROCTOR.Application.DTOs.Cases;

public class CaseHearingPersonDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "internal";
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? UserId { get; set; }
    public string? Role { get; set; }
    public string AddedAt { get; set; } = string.Empty;
}

public class AddInternalHearingPersonRequest
{
    public string UserId { get; set; } = string.Empty;
}

public class AddExternalHearingPersonRequest
{
    public List<string> Emails { get; set; } = new();
    public string? Name { get; set; }
    public string? Subject { get; set; }
    public string Message { get; set; } = string.Empty;
}
