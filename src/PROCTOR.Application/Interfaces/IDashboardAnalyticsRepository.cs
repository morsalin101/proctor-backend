using PROCTOR.Application.DTOs.Dashboard;

namespace PROCTOR.Application.Interfaces;

public interface IDashboardAnalyticsRepository
{
    Task<DashboardAnalyticsDto> GetAsync(DashboardFilter filter, string role, Guid? userId);
}
