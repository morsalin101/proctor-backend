using PROCTOR.Application.DTOs.Dashboard;

namespace PROCTOR.Application.Interfaces;

public interface IDashboardAnalyticsRepository
{
    Task<DashboardAnalyticsDto> GetAsync(DashboardFilter filter, string role, Guid? userId);
    Task<ReportQueueCountsDto> GetReportQueueCountsAsync(string role, Guid? userId);
    Task<AssignmentWorkloadDto> GetAssignmentWorkloadAsync(string role, Guid? userId);
}
