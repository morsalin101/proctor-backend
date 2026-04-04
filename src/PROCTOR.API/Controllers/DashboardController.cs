using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROCTOR.Application.Interfaces;

namespace PROCTOR.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var response = await _dashboardService.GetStatsAsync();
        return Ok(response);
    }

    [HttpGet("recent-activity")]
    public async Task<IActionResult> GetRecentActivity()
    {
        var response = await _dashboardService.GetRecentActivityAsync();
        return Ok(response);
    }
}
