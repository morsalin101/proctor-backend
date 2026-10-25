using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Dashboard;

namespace PROCTOR.Application.Interfaces;

public interface IDashboardService
{
    Task<ApiResponse<DashboardStatsDto>> GetStatsAsync();
    Task<ApiResponse<List<RecentActivityDto>>> GetRecentActivityAsync();
    Task<ApiResponse<DashboardAnalyticsDto>> GetAnalyticsAsync(DashboardFilter filter, string role, Guid? userId);
    Task<ApiResponse<ReportQueueCountsDto>> GetReportQueueCountsAsync(string role, Guid? userId);
    Task<ApiResponse<AssignmentWorkloadDto>> GetAssignmentWorkloadAsync(string role, Guid? userId);
}
