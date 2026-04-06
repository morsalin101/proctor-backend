using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROCTOR.Application.Common;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Interfaces;

namespace PROCTOR.API.Controllers;

[ApiController]
[Route("api/verification-checklist")]
[Authorize]
[Produces("application/json")]
public class VerificationChecklistController : ControllerBase
{
    private readonly IRepository<VerificationChecklistItem> _checklistRepo;
    private readonly IRepository<CaseVerification> _verificationRepo;
    private readonly IUnitOfWork _unitOfWork;

    public VerificationChecklistController(
        IRepository<VerificationChecklistItem> checklistRepo,
        IRepository<CaseVerification> verificationRepo,
        IUnitOfWork unitOfWork)
    {
        _checklistRepo = checklistRepo;
        _verificationRepo = verificationRepo;
        _unitOfWork = unitOfWork;
    }

    private string GetCurrentUserRole() =>
        User.FindFirst("role")?.Value ?? "";

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _checklistRepo.FindAsync(i => i.IsActive);
        var sorted = items.OrderBy(i => i.Order).Select(i => new
        {
            id = i.Id.ToString(),
            label = i.Label,
            order = i.Order,
            isActive = i.IsActive
        });
        return Ok(ApiResponse<object>.SuccessResponse(sorted));
    }

    [HttpPost]
    [Authorize(Roles = "super-admin")]
    public async Task<IActionResult> Create([FromBody] ChecklistItemRequest request)
    {
        var allItems = await _checklistRepo.GetAllAsync();
        var maxOrder = allItems.Any() ? allItems.Max(i => i.Order) : 0;

        var item = new VerificationChecklistItem
        {
            Id = Guid.NewGuid(),
            Label = request.Label,
            Order = maxOrder + 1,
            IsActive = true
        };

        await _checklistRepo.AddAsync(item);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResponse(new { id = item.Id.ToString(), label = item.Label, order = item.Order }, "Checklist item created."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "super-admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ChecklistItemRequest request)
    {
        var item = await _checklistRepo.GetByIdAsync(id);
        if (item is null)
            return NotFound(ApiResponse<object>.FailResponse("Item not found."));

        item.Label = request.Label;
        if (request.Order.HasValue) item.Order = request.Order.Value;
        item.UpdatedAt = DateTime.UtcNow;
        _checklistRepo.Update(item);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResponse(new { id = item.Id.ToString(), label = item.Label, order = item.Order }, "Updated."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "super-admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _checklistRepo.GetByIdAsync(id);
        if (item is null)
            return NotFound(ApiResponse<object>.FailResponse("Item not found."));

        item.IsActive = false;
        item.UpdatedAt = DateTime.UtcNow;
        _checklistRepo.Update(item);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Deleted."));
    }

    // Case verification endpoints
    [HttpPost("case/{caseId:guid}")]
    public async Task<IActionResult> CreateVerification(Guid caseId, [FromBody] CaseVerificationRequest request)
    {
        var userName = User.FindFirst("name")?.Value ?? "Unknown";

        var verification = new CaseVerification
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            Comment = request.Comment,
            ChecklistResultsJson = request.ChecklistResultsJson ?? "[]",
            CreatedByName = userName
        };

        await _verificationRepo.AddAsync(verification);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            id = verification.Id.ToString(),
            comment = verification.Comment,
            checklistResultsJson = verification.ChecklistResultsJson,
            createdByName = verification.CreatedByName,
            createdAt = verification.CreatedAt.ToString("o")
        }, "Verification created."));
    }

    [HttpGet("case/{caseId:guid}")]
    public async Task<IActionResult> GetVerifications(Guid caseId)
    {
        var verifications = await _verificationRepo.FindAsync(v => v.CaseId == caseId);
        var result = verifications.OrderByDescending(v => v.CreatedAt).Select(v => new
        {
            id = v.Id.ToString(),
            comment = v.Comment,
            checklistResultsJson = v.ChecklistResultsJson,
            createdByName = v.CreatedByName,
            createdAt = v.CreatedAt.ToString("o")
        });
        return Ok(ApiResponse<object>.SuccessResponse(result));
    }
}

public class ChecklistItemRequest
{
    public string Label { get; set; } = string.Empty;
    public int? Order { get; set; }
}

public class CaseVerificationRequest
{
    public string Comment { get; set; } = string.Empty;
    public string? ChecklistResultsJson { get; set; }
}
