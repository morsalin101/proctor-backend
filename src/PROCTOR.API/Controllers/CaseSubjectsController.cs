using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.CaseSubjects;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Interfaces;

namespace PROCTOR.API.Controllers;

[ApiController]
[Route("api/case-subjects")]
[Authorize]
[Produces("application/json")]
public class CaseSubjectsController : ControllerBase
{
    private readonly IRepository<CaseSubject> _subjects;
    private readonly IUnitOfWork _unitOfWork;

    public CaseSubjectsController(IRepository<CaseSubject> subjects, IUnitOfWork unitOfWork)
    {
        _subjects = subjects;
        _unitOfWork = unitOfWork;
    }

    private static CaseSubjectDto ToDto(CaseSubject s) => new()
    {
        Id = s.Id.ToString(),
        Subject = s.Subject,
        Order = s.Order,
        IsActive = s.IsActive
    };

    // Any authenticated user can read the suggestion list (used by the Type-2 form).
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var all = await _subjects.GetAllAsync();
        var dtos = all
            .Where(s => includeInactive || s.IsActive)
            .OrderBy(s => s.Order).ThenBy(s => s.Subject)
            .Select(ToDto).ToList();
        return Ok(ApiResponse<List<CaseSubjectDto>>.SuccessResponse(dtos));
    }

    [HttpPost]
    [Authorize(Roles = "super-admin")]
    public async Task<IActionResult> Create([FromBody] CreateCaseSubjectRequest request)
    {
        var subject = (request.Subject ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(subject))
            return BadRequest(ApiResponse<CaseSubjectDto>.FailResponse("Subject is required."));

        var existing = await _subjects.FindAsync(s => s.Subject.ToLower() == subject.ToLower());
        if (existing.Any())
            return BadRequest(ApiResponse<CaseSubjectDto>.FailResponse("That subject already exists."));

        var entity = new CaseSubject
        {
            Id = Guid.NewGuid(),
            Subject = subject,
            Order = request.Order,
            IsActive = true
        };
        await _subjects.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<CaseSubjectDto>.SuccessResponse(ToDto(entity), "Subject added."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "super-admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCaseSubjectRequest request)
    {
        var entity = await _subjects.GetByIdAsync(id);
        if (entity is null)
            return NotFound(ApiResponse<CaseSubjectDto>.FailResponse("Subject not found."));

        if (!string.IsNullOrWhiteSpace(request.Subject)) entity.Subject = request.Subject.Trim();
        if (request.Order.HasValue) entity.Order = request.Order.Value;
        if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
        _subjects.Update(entity);
        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<CaseSubjectDto>.SuccessResponse(ToDto(entity), "Subject updated."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "super-admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _subjects.GetByIdAsync(id);
        if (entity is null)
            return NotFound(ApiResponse<CaseSubjectDto>.FailResponse("Subject not found."));
        _subjects.Remove(entity);
        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Subject deleted."));
    }
}
