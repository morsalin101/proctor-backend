using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROCTOR.Application.Interfaces;
using PROCTOR.Application.DTOs.Dashboard;
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
}
