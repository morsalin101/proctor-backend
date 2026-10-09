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
    private readonly ICaseService _caseService;
    private readonly IPermissionChecker _permissionChecker;

    public AiController(
        IAiService aiService,
        IForwardingRuleService forwardingRuleService,
        ICaseService caseService,
        IPermissionChecker permissionChecker)
    {
        _aiService = aiService;
        _forwardingRuleService = forwardingRuleService;
        _caseService = caseService;
        _permissionChecker = permissionChecker;
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

    // AI drafting follows the same authorization rule as manual report creation.
    [HttpPost("cases/{caseId:guid}/report")]
    public async Task<IActionResult> GenerateReport(Guid caseId, [FromBody] GenerateReportRequest request)
    {
        var role = GetCurrentUserRole();
        var caseResponse = await _caseService.GetCaseByIdAsync(caseId, role);
        if (!caseResponse.Success || caseResponse.Data is null)
            return NotFound(caseResponse);
        var canCreateFromMenu = await _permissionChecker.HasPermissionAsync(role, "reports", "create");
        var special = await _forwardingRuleService.GetSpecialPermissionsAsync(role, caseResponse.Data.Type);
        if (!canCreateFromMenu && !(special.Data?.CanDraftReport ?? false))
            return StatusCode(403, ApiResponse<object>.FailResponse(
                "Your role cannot create reports. Enable Pending Reports → Create or the case-type Create draft report permission."));

        var response = await _aiService.GenerateCaseReportAsync(caseId, request);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }
}
