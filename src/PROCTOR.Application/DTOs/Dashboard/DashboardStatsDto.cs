namespace PROCTOR.Application.DTOs.Dashboard;

public class DashboardStatsDto
{
    public int TotalCases { get; set; }
    public int PendingCases { get; set; }
    public int UnderReview { get; set; }
    public int ResolvedCases { get; set; }
}
