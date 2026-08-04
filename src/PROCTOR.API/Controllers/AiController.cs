using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Ai;
using PROCTOR.Application.Interfaces;

namespace PROCTOR.API.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
[Produces("application/json")]
public class AiController : ControllerBase
{
    private readonly IAiService _aiService;
    private readonly IForwardingRuleService _forwardingRuleService;

    public AiController(IAiService aiService, IForwardingRuleService forwardingRuleService)
    {
        _aiService = aiService;
        _forwardingRuleService = forwardingRuleService;
    }

    private string GetCurrentUserRole() => User.FindFirst("role")?.Value ?? "";

    // Provider config is an admin-only concern — it holds a billable API credential.
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        if (GetCurrentUserRole() != "super-admin") return Forbid();
        return Ok(await _aiService.GetSettingsAsync());
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateAiSettingsRequest request)
    {
        if (GetCurrentUserRole() != "super-admin") return Forbid();
        return Ok(await _aiService.UpdateSettingsAsync(request));
    }

    [HttpPost("test")]
    public async Task<IActionResult> TestConnection([FromBody] TestAiConnectionRequest request)
    {
        if (GetCurrentUserRole() != "super-admin") return Forbid();
        return Ok(await _aiService.TestConnectionAsync(request));
    }

    // Drafting a report is gated by the same __draft_report__ permission as writing one by hand.
    [HttpPost("cases/{caseId:guid}/report")]
    public async Task<IActionResult> GenerateReport(Guid caseId, [FromBody] GenerateReportRequest request)
    {
        var role = GetCurrentUserRole();
        var special = await _forwardingRuleService.GetSpecialPermissionsAsync(role);
        if (!(special.Data?.CanDraftReport ?? false))
            return StatusCode(403, ApiResponse<object>.FailResponse("Your role does not have draft report permission."));

        var response = await _aiService.GenerateCaseReportAsync(caseId, request);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }
}
