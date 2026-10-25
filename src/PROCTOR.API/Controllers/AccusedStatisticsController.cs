using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.AccusedStatistics;
using PROCTOR.Application.Interfaces;

namespace PROCTOR.API.Controllers;

[ApiController]
[Route("api/accused-statistics")]
[Authorize]
[Produces("application/json")]
public class AccusedStatisticsController : ControllerBase
{
    private readonly IAccusedStatisticsRepository _statistics;
    private readonly IPermissionChecker _permissions;

    public AccusedStatisticsController(IAccusedStatisticsRepository statistics, IPermissionChecker permissions)
    {
        _statistics = statistics;
        _permissions = permissions;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] AccusedStatisticsFilter filter)
    {
        var role = User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        if (!await _permissions.HasPermissionAsync(role, "accused-statistics", "read")) return Forbid();
        var rawId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userId = Guid.TryParse(rawId, out var id) ? id : (Guid?)null;
        var data = await _statistics.GetAsync(filter, role, userId);
        return Ok(ApiResponse<AccusedStatisticsDto>.SuccessResponse(data));
    }
}
