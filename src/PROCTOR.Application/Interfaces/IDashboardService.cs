using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Dashboard;

namespace PROCTOR.Application.Interfaces;

public interface IDashboardService
{
    Task<ApiResponse<DashboardStatsDto>> GetStatsAsync();
    Task<ApiResponse<List<RecentActivityDto>>> GetRecentActivityAsync();
}
