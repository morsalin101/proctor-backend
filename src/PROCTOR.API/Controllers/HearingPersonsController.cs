using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Cases;
using PROCTOR.Application.Interfaces;
using PROCTOR.Application.Mapping;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Interfaces;

namespace PROCTOR.API.Controllers;

[ApiController]
[Route("api/cases/{caseId:guid}/hearing-persons")]
[Authorize]
[Produces("application/json")]
public class HearingPersonsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;

    public HearingPersonsController(IUnitOfWork unitOfWork, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
    }

    private static CaseHearingPersonDto ToDto(CaseHearingPerson p) => new()
    {
        Id = p.Id, Type = p.Type, Name = p.Name, Email = p.Email,
        UserId = p.UserId, Role = p.Role, AddedAt = p.AddedAt.ToString("o")
    };

    // Add an internal system user to the hearing panel.
    [HttpPost("internal")]
    public async Task<IActionResult> AddInternal(Guid caseId, [FromBody] AddInternalHearingPersonRequest request)
    {
        var c = await _unitOfWork.Cases.GetByIdAsync(caseId);
        if (c is null) return NotFound(ApiResponse<object>.FailResponse("Case not found."));

        if (!Guid.TryParse(request.UserId, out var uid))
            return BadRequest(ApiResponse<object>.FailResponse("Invalid user id."));

        var user = await _unitOfWork.Users.GetByIdAsync(uid);
        if (user is null) return NotFound(ApiResponse<object>.FailResponse("User not found."));

        if (c.HearingPersons.Any(p => p.Type == "internal" && p.UserId == user.Id.ToString()))
            return BadRequest(ApiResponse<object>.FailResponse("That person is already on the hearing panel."));

        var person = new CaseHearingPerson
        {
            Id = Guid.NewGuid().ToString(),
            Type = "internal",
            Name = user.Name,
            Email = user.Email,
            UserId = user.Id.ToString(),
            Role = user.Role.ToKebabCase(),
            AddedAt = DateTime.UtcNow
        };
        c.HearingPersons = new List<CaseHearingPerson>(c.HearingPersons) { person };
        c.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<CaseHearingPersonDto>.SuccessResponse(ToDto(person), "Person added to hearing panel."));
    }

    // Add one or more external people (email only) and send them a (demo) notification.
    [HttpPost("external")]
    public async Task<IActionResult> AddExternal(Guid caseId, [FromBody] AddExternalHearingPersonRequest request)
    {
        var c = await _unitOfWork.Cases.GetByIdAsync(caseId);
        if (c is null) return NotFound(ApiResponse<object>.FailResponse("Case not found."));

        var emails = (request.Emails ?? new List<string>())
            .Select(e => e?.Trim() ?? string.Empty)
            .Where(e => e.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (emails.Count == 0)
            return BadRequest(ApiResponse<object>.FailResponse("At least one email is required."));
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(ApiResponse<object>.FailResponse("A message is required."));

        var subject = string.IsNullOrWhiteSpace(request.Subject)
            ? $"Hearing notification — {c.CaseNumber}"
            : request.Subject!.Trim();
        var body = $"{request.Message}\n\nThis is a demo email — no real SMTP is configured yet.";

        var added = new List<CaseHearingPerson>(c.HearingPersons);
        foreach (var email in emails)
        {
            await _emailService.SendAsync(email, subject, body, c.Id);
            added.Add(new CaseHearingPerson
            {
                Id = Guid.NewGuid().ToString(),
                Type = "external",
                Name = string.IsNullOrWhiteSpace(request.Name) ? email : request.Name!.Trim(),
                Email = email,
                AddedAt = DateTime.UtcNow
            });
        }
        c.HearingPersons = added;
        c.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<List<CaseHearingPersonDto>>.SuccessResponse(
            added.Where(p => emails.Contains(p.Email ?? "", StringComparer.OrdinalIgnoreCase)).Select(ToDto).ToList(),
            $"Added {emails.Count} external person(s) and sent email."));
    }

    [HttpDelete("{personId}")]
    public async Task<IActionResult> Remove(Guid caseId, string personId)
    {
        var c = await _unitOfWork.Cases.GetByIdAsync(caseId);
        if (c is null) return NotFound(ApiResponse<object>.FailResponse("Case not found."));

        c.HearingPersons = c.HearingPersons.Where(p => p.Id != personId).ToList();
        c.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Person removed."));
    }
}
