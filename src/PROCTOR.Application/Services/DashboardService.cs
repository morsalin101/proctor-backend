using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Dashboard;
using PROCTOR.Application.Interfaces;
using PROCTOR.Application.Mapping;
using PROCTOR.Domain.Enums;
using PROCTOR.Domain.Interfaces;

namespace PROCTOR.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<DashboardStatsDto>> GetStatsAsync()
    {
        var totalCases = await _unitOfWork.Cases.CountAsync();

        var pendingCases = await _unitOfWork.Cases.CountAsync(c =>
            c.Status == CaseStatus.Submitted || c.Status == CaseStatus.Pending);

        var underReview = await _unitOfWork.Cases.CountAsync(c =>
            c.Status == CaseStatus.UnderReview || c.Status == CaseStatus.Verified || c.Status == CaseStatus.Assigned);

        var resolvedCases = await _unitOfWork.Cases.CountAsync(c =>
            c.Status == CaseStatus.Resolved || c.Status == CaseStatus.Closed);

        var stats = new DashboardStatsDto
        {
            TotalCases = totalCases,
            PendingCases = pendingCases,
            UnderReview = underReview,
            ResolvedCases = resolvedCases
        };

        return ApiResponse<DashboardStatsDto>.SuccessResponse(stats);
    }

    public async Task<ApiResponse<List<RecentActivityDto>>> GetRecentActivityAsync()
    {
        // Get all cases with details to access timeline events
        var cases = await _unitOfWork.Cases.GetFilteredAsync(null, null, null, null, 1, int.MaxValue);

        var allEvents = new List<Domain.Entities.TimelineEvent>();
        foreach (var c in cases)
        {
            // Need to load with details to get timeline events
            var detailed = await _unitOfWork.Cases.GetByIdWithDetailsAsync(c.Id);
            if (detailed is not null)
                allEvents.AddRange(detailed.TimelineEvents);
        }

        var recentActivities = allEvents
            .OrderByDescending(e => e.CreatedAt)
            .Take(20)
            .Select(e => e.ToDto())
            .ToList();

        return ApiResponse<List<RecentActivityDto>>.SuccessResponse(recentActivities);
    }
}
