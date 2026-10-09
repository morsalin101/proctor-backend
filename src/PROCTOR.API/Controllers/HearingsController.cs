using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROCTOR.Application.Common;
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
    private readonly ICaseService _caseService;
    private readonly IForwardingRuleService _forwardingRuleService;

    public HearingsController(IHearingService hearingService, ICaseService caseService, IForwardingRuleService forwardingRuleService)
    {
        _hearingService = hearingService;
        _caseService = caseService;
        _forwardingRuleService = forwardingRuleService;
    }

    private string GetCurrentUserId() =>
        User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    private string GetCurrentUserRole() =>
        User.FindFirst("role")?.Value ?? "";

    private string GetCurrentUserName() =>
        User.FindFirst("name")?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "System";

    [HttpGet]
    public async Task<IActionResult> GetHearings([FromQuery] Guid? caseId)
    {
        var response = await _hearingService.GetHearingsAsync(caseId, GetCurrentUserRole());
        return Ok(response);
    }

    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcoming([FromQuery] bool mineOnly = false)
    {
        Guid? filterUserId = null;
        if (mineOnly)
        {
            var role = GetCurrentUserRole();
            // Coordinators / proctors / super-admin see everyone's upcoming hearings
            // (coordinators are still gender-filtered downstream in HearingService).
            // Anyone else is filtered to their own assignments.
            if (role != "coordinator" && role != "female-coordinator" && role != "proctor" && role != "super-admin")
            {
                if (Guid.TryParse(GetCurrentUserId(), out var uid))
                    filterUserId = uid;
            }
        }

        var response = await _hearingService.GetUpcomingHearingsAsync(filterUserId, GetCurrentUserRole());
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
        if (!Guid.TryParse(request.CaseId, out var caseId))
            return BadRequest(ApiResponse<object>.FailResponse("Invalid case."));

        var role = GetCurrentUserRole();
        var caseResponse = await _caseService.GetCaseByIdAsync(caseId, role);
        if (!caseResponse.Success || caseResponse.Data is null)
            return NotFound(caseResponse);
        var special = await _forwardingRuleService.GetSpecialPermissionsAsync(role, caseResponse.Data.Type);
        if (!(special.Data?.CanHearing ?? false))
            return StatusCode(403, ApiResponse<object>.FailResponse("Your role does not have hearing permission for this case type."));

        Guid? createdById = Guid.TryParse(GetCurrentUserId(), out var uid) ? uid : null;
        var response = await _hearingService.CreateHearingAsync(request, createdById, GetCurrentUserName());
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

    [HttpPost("{id:guid}/notify-email")]
    public async Task<IActionResult> SendHearingEmail(Guid id, [FromBody] NotifyHearingEmailRequest request)
    {
        var response = await _hearingService.SendHearingEmailAsync(id, request, GetCurrentUserName());
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateHearingStatus(Guid id, [FromBody] UpdateHearingStatusRequest request)
    {
        Guid? actingUserId = Guid.TryParse(GetCurrentUserId(), out var uid) ? uid : null;
        var response = await _hearingService.UpdateHearingStatusAsync(id, request.Status, actingUserId, GetCurrentUserName());
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    // Move a scheduled hearing to a new slot. The original slot and the reason are kept
    // in the hearing's reschedule history and on the case timeline.
    [HttpPost("{id:guid}/reschedule")]
    public async Task<IActionResult> RescheduleHearing(Guid id, [FromBody] RescheduleHearingRequest request)
    {
        Guid? actingUserId = Guid.TryParse(GetCurrentUserId(), out var uid) ? uid : null;
        var response = await _hearingService.RescheduleHearingAsync(id, request, actingUserId, GetCurrentUserName());
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }
}

public class UpdateHearingStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
