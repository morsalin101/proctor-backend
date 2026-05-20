using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROCTOR.Application.DTOs.Hearings;
using PROCTOR.Application.Interfaces;

namespace PROCTOR.API.Controllers;

[ApiController]
[Route("api/hearings")]
[Authorize]
[Produces("application/json")]
public class HearingsController : ControllerBase
{
    private readonly IHearingService _hearingService;

    public HearingsController(IHearingService hearingService)
    {
        _hearingService = hearingService;
    }

    private string GetCurrentUserId() =>
        User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    private string GetCurrentUserRole() =>
        User.FindFirst("role")?.Value ?? "";

    [HttpGet]
    public async Task<IActionResult> GetHearings([FromQuery] Guid? caseId)
    {
        var response = await _hearingService.GetHearingsAsync(caseId);
        return Ok(response);
    }

    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcoming([FromQuery] bool mineOnly = false)
    {
        Guid? filterUserId = null;
        if (mineOnly)
        {
            var role = GetCurrentUserRole();
            // Coordinators / super-admin see everyone's upcoming hearings.
            // Anyone else is filtered to their own assignments.
            if (role != "coordinator" && role != "super-admin")
            {
                if (Guid.TryParse(GetCurrentUserId(), out var uid))
                    filterUserId = uid;
            }
        }

        var response = await _hearingService.GetUpcomingHearingsAsync(filterUserId);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetHearingById(Guid id)
    {
        var response = await _hearingService.GetHearingByIdAsync(id);
        if (!response.Success)
            return NotFound(response);

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> CreateHearing([FromBody] CreateHearingRequest request)
    {
        var response = await _hearingService.CreateHearingAsync(request);
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateHearing(Guid id, [FromBody] UpdateHearingRequest request)
    {
        var response = await _hearingService.UpdateHearingAsync(id, request);
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateHearingStatus(Guid id, [FromBody] UpdateHearingStatusRequest request)
    {
        var response = await _hearingService.UpdateHearingStatusAsync(id, request.Status);
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }
}

public class UpdateHearingStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
