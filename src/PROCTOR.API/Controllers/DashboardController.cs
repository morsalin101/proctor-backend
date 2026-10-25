using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROCTOR.Application.Interfaces;
using PROCTOR.Application.DTOs.Dashboard;
using PROCTOR.Application.Common;
using System.Security.Claims;

namespace PROCTOR.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IPermissionChecker _permissionChecker;

    public DashboardController(IDashboardService dashboardService, IPermissionChecker permissionChecker)
    {
        _dashboardService = dashboardService;
        _permissionChecker = permissionChecker;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var response = await _dashboardService.GetStatsAsync();
        return Ok(response);
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics([FromQuery] DashboardFilter filter)
    {
        var role = User.FindFirst("role")?.Value ?? "";
        if (!await _permissionChecker.HasPermissionAsync(role, "advanced-search", "read"))
            return Forbid();
        var rawId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userId = Guid.TryParse(rawId, out var id) ? id : (Guid?)null;
        return Ok(await _dashboardService.GetAnalyticsAsync(filter, role, userId));
    }

    [HttpGet("recent-activity")]
    public async Task<IActionResult> GetRecentActivity()
    {
        var response = await _dashboardService.GetRecentActivityAsync();
        return Ok(response);
    }

    [HttpGet("report-counts")]
    public async Task<IActionResult> GetReportCounts()
    {
        var role = User.FindFirst("role")?.Value ?? "";
        var rawId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userId = Guid.TryParse(rawId, out var id) ? id : (Guid?)null;
        var response = await _dashboardService.GetReportQueueCountsAsync(role, userId);
        if (response.Data is not null && role != "super-admin")
        {
            if (!await _permissionChecker.HasPermissionAsync(role, "reports", "read"))
                response.Data.Pending = 0;
            if (!await _permissionChecker.HasPermissionAsync(role, "completed-reports", "read"))
                response.Data.Completed = 0;
        }
        return Ok(response);
    }

    [HttpGet("assignment-workload")]
    public async Task<IActionResult> GetAssignmentWorkload()
    {
        var role = User.FindFirst("role")?.Value ?? "";
        if (role is "student" or "external")
            return Ok(ApiResponse<AssignmentWorkloadDto>.SuccessResponse(new AssignmentWorkloadDto()));
        if (!await _permissionChecker.HasPermissionAsync(role, "dashboard", "read"))
            return Forbid();
        var rawId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userId = Guid.TryParse(rawId, out var id) ? id : (Guid?)null;
        return Ok(await _dashboardService.GetAssignmentWorkloadAsync(role, userId));
    }
}
