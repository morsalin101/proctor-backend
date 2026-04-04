namespace PROCTOR.Application.DTOs.Notifications;

public class NotificationDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public string? CaseId { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}
