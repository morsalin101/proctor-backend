using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Cases;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Interfaces;

namespace PROCTOR.API.Controllers;

[ApiController]
[Route("api/cases/{caseId:guid}/additional-info")]
[Authorize]
[Produces("application/json")]
public class AdditionalInfoController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AdditionalInfoController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private string GetCurrentUserName() => User.FindFirst("name")?.Value ?? "Unknown";
    private string GetCurrentUserRole() => User.FindFirst("role")?.Value ?? "";

    [HttpPost]
    public async Task<IActionResult> Add(Guid caseId, [FromBody] AddAdditionalInfoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(ApiResponse<object>.FailResponse("Content is required."));

        var caseEntity = await _unitOfWork.Cases.GetByIdAsync(caseId);
        if (caseEntity is null)
            return NotFound(ApiResponse<object>.FailResponse("Case not found."));

        var author = GetCurrentUserName();
        var role = GetCurrentUserRole();

        var info = new CaseAdditionalInfo
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            Content = request.Content.Trim(),
            Author = author,
            AuthorRole = role
        };
        // Add via the context directly (entity carries CaseId) — the reliable insert pattern.
        _unitOfWork.Add(info);

        _unitOfWork.Add(new TimelineEvent
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            Action = "Additional Information Added",
            Description = $"Additional information added by {author}.",
            User = author
        });

        await _unitOfWork.SaveChangesAsync();

        var dto = new AdditionalInfoDto
        {
            Id = info.Id.ToString(),
            Content = info.Content,
            Author = info.Author,
            AuthorRole = info.AuthorRole,
            CreatedDate = info.CreatedAt.ToString("o")
        };
        return Ok(ApiResponse<AdditionalInfoDto>.SuccessResponse(dto, "Additional information added."));
    }
}
