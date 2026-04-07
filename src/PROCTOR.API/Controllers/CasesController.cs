using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROCTOR.Application.DTOs.Cases;
using PROCTOR.Application.DTOs.Reports;
using PROCTOR.Application.Interfaces;

namespace PROCTOR.API.Controllers;

[ApiController]
[Route("api/cases")]
[Authorize]
[Produces("application/json")]
public class CasesController : ControllerBase
{
    private readonly ICaseService _caseService;
    private readonly IPermissionChecker _permissionChecker;

    public CasesController(ICaseService caseService, IPermissionChecker permissionChecker)
    {
        _caseService = caseService;
        _permissionChecker = permissionChecker;
    }

    private string GetCurrentUserName() =>
        User.FindFirst("name")?.Value ?? "Unknown";

    private string GetCurrentUserRole() =>
        User.FindFirst("role")?.Value ?? "";

    private string GetCurrentUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    [HttpGet]
    public async Task<IActionResult> GetCases(
        [FromQuery] string? status,
        [FromQuery] string? type,
        [FromQuery] string? priority,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var response = await _caseService.GetCasesAsync(status, type, priority, search, page, pageSize);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCaseById(Guid id)
    {
        var response = await _caseService.GetCaseByIdAsync(id);
        if (!response.Success)
            return NotFound(response);

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCase([FromBody] CreateCaseRequest request)
    {
        var createdBy = GetCurrentUserName();
        var userIdStr = GetCurrentUserId();
        var submittedByUserId = Guid.TryParse(userIdStr, out var uid) ? uid : (Guid?)null;
        var response = await _caseService.CreateCaseAsync(request, createdBy, submittedByUserId);
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCase(Guid id, [FromBody] UpdateCaseRequest request)
    {
        var response = await _caseService.UpdateCaseAsync(id, request);
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateCaseStatus(Guid id, [FromBody] UpdateCaseStatusRequest request)
    {
        var updatedBy = GetCurrentUserName();
        var userRole = GetCurrentUserRole();
        var response = await _caseService.UpdateCaseStatusAsync(id, request, updatedBy, userRole);
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPost("{id:guid}/forward")]
    [Authorize(Roles = "coordinator,proctor,assistant-proctor,deputy-proctor,registrar,disciplinary-committee,female-coordinator,sexual-harassment-committee,super-admin")]
    public async Task<IActionResult> ForwardCase(Guid id, [FromBody] ForwardCaseRequest request)
    {
        var updatedBy = GetCurrentUserName();
        var userRole = GetCurrentUserRole();
        var response = await _caseService.ForwardCaseAsync(id, request, updatedBy, userRole);
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPost("{id:guid}/reports")]
    public async Task<IActionResult> CreateReport(Guid id, [FromBody] CreateReportRequest request)
    {
        var createdByName = GetCurrentUserName();
        var userIdStr = GetCurrentUserId();
        var createdById = Guid.TryParse(userIdStr, out var uid) ? uid : Guid.Empty;
        var response = await _caseService.CreateReportAsync(id, request, createdByName, createdById);
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPut("{caseId:guid}/reports/{reportId:guid}")]
    public async Task<IActionResult> UpdateReport(Guid caseId, Guid reportId, [FromBody] CreateReportRequest request)
    {
        var createdByName = GetCurrentUserName();
        var response = await _caseService.UpdateReportAsync(caseId, reportId, request, createdByName);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("{id:guid}/reports")]
    public async Task<IActionResult> GetReports(Guid id)
    {
        var response = await _caseService.GetReportsAsync(id);
        if (!response.Success)
            return NotFound(response);

        return Ok(response);
    }

    [HttpGet("my-cases")]
    public async Task<IActionResult> GetMyCases([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userIdStr = GetCurrentUserId();
        var userId = Guid.TryParse(userIdStr, out var uid) ? uid : Guid.Empty;
        var userRole = GetCurrentUserRole();
        var response = await _caseService.GetMyCasesAsync(userId, userRole, page, pageSize);
        return Ok(response);
    }

    [HttpGet("my-cases/count")]
    public async Task<IActionResult> GetMyCasesCount()
    {
        var userIdStr = GetCurrentUserId();
        var userId = Guid.TryParse(userIdStr, out var uid) ? uid : Guid.Empty;
        var userRole = GetCurrentUserRole();
        var response = await _caseService.GetMyCasesCountAsync(userId, userRole);
        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCase(Guid id)
    {
        var userRole = GetCurrentUserRole();
        var canDelete = await _permissionChecker.HasPermissionAsync(userRole, "cases", "delete");
        if (!canDelete)
            return Forbid();

        var response = await _caseService.DeleteCaseAsync(id);
        if (!response.Success)
            return NotFound(response);

        return Ok(response);
    }
}
