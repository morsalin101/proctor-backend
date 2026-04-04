using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROCTOR.Application.DTOs.Cases;
using PROCTOR.Application.Interfaces;

namespace PROCTOR.API.Controllers;

[ApiController]
[Route("api/cases")]
[Authorize]
[Produces("application/json")]
public class CasesController : ControllerBase
{
    private readonly ICaseService _caseService;

    public CasesController(ICaseService caseService)
    {
        _caseService = caseService;
    }

    private string GetCurrentUserName() =>
        User.FindFirst("name")?.Value ?? "Unknown";

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
        var response = await _caseService.CreateCaseAsync(request, createdBy);
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
        var response = await _caseService.UpdateCaseStatusAsync(id, request, updatedBy);
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }
}
