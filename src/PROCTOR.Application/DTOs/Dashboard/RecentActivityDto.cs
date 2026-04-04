namespace PROCTOR.Application.DTOs.Dashboard;

public class RecentActivityDto
{
    public string Id { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
}
