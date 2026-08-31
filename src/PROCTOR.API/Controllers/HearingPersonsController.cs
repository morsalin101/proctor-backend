using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Cases;
using PROCTOR.Application.Interfaces;
using PROCTOR.Application.Mapping;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
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
    private readonly INotificationService _notificationService;

    public HearingPersonsController(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _notificationService = notificationService;
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

        // Notify the added person so they know they've been pulled onto the panel
        // (matches the "external" path which emails recipients right away).
        await _notificationService.CreateAsync(
            user.Id,
            null,
            "Added to Hearing Panel",
            $"You have been added to the hearing panel for case {c.CaseNumber}.",
            c.Id);

        return Ok(ApiResponse<CaseHearingPersonDto>.SuccessResponse(ToDto(person), "Person added to hearing panel."));
    }

    // Add one or more external people (email only) and send them a (demo) notification.
    [HttpPost("external")]
    public async Task<IActionResult> AddExternal(Guid caseId, [FromBody] AddExternalHearingPersonRequest request)
    {
        // Loaded with details so existing Assignments are populated — AssignToCase must be
        // able to see a prior assignment instead of inserting a duplicate row.
        var c = await _unitOfWork.Cases.GetByIdWithDetailsAsync(caseId);
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

        var added = new List<CaseHearingPerson>(c.HearingPersons);
        foreach (var email in emails)
        {
            // Every external person called to a hearing gets a real profile so they can
            // sign in and follow the case they were called for. An existing account with
            // the same email is reused rather than duplicated.
            var (user, generatedPassword) = await GetOrCreateExternalUserAsync(email, request.Name);
            AssignToCase(c, user.Id);

            var body = $"{request.Message}\n\nCase: {c.CaseNumber}";
            if (generatedPassword is not null)
            {
                body += $"\n\nAn account has been created for you so you can follow this case."
                      + $"\nSign in with:\n  Email: {user.Email}\n  Temporary password: {generatedPassword}"
                      + $"\nPlease change your password after your first sign-in.";
            }
            body += "\n\nThis is a demo email — no real SMTP is configured yet.";

            await _emailService.SendAsync(email, subject, body, c.Id);

            await _notificationService.CreateAsync(user.Id, null,
                "Added to Hearing Panel",
                $"You have been called to the hearing for case {c.CaseNumber}.", c.Id);

            added.Add(new CaseHearingPerson
            {
                Id = Guid.NewGuid().ToString(),
                Type = "external",
                Name = user.Name,
                Email = email,
                UserId = user.Id.ToString(),
                Role = user.Role.ToKebabCase(),
                AddedAt = DateTime.UtcNow
            });
        }
        c.HearingPersons = added;
        c.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<List<CaseHearingPersonDto>>.SuccessResponse(
            added.Where(p => emails.Contains(p.Email ?? "", StringComparer.OrdinalIgnoreCase)).Select(ToDto).ToList(),
            $"Added {emails.Count} external person(s), created their profiles and sent email."));
    }

    /// <summary>
    /// Finds the account for an external participant, creating one under the External role
    /// if it does not exist. Returns the generated password only when a new account was made.
    /// </summary>
    private async Task<(User User, string? GeneratedPassword)> GetOrCreateExternalUserAsync(string email, string? name)
    {
        var existing = await _unitOfWork.Users.GetByEmailAsync(email);
        if (existing is not null) return (existing, null);

        var password = GenerateTemporaryPassword();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Name = string.IsNullOrWhiteSpace(name) ? email.Split('@')[0] : name.Trim(),
            Role = UserRole.External,
            Gender = Gender.Unspecified,
            RankName = "External Participant",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _unitOfWork.Add(user);
        return (user, password);
    }

    /// <summary>Links a user to the case as an active (non-primary) assignee, if not already linked.</summary>
    private void AssignToCase(Case c, Guid userId)
    {
        var existing = c.Assignments.FirstOrDefault(a => a.UserId == userId);
        if (existing is not null)
        {
            existing.IsActive = true;
            return;
        }

        // Added through the unit of work (not just the parent's navigation collection) so EF
        // tracks the client-set key as Added → INSERT rather than an UPDATE matching 0 rows.
        var assignment = new CaseAssignment
        {
            Id = Guid.NewGuid(),
            CaseId = c.Id,
            UserId = userId,
            AssignedAt = DateTime.UtcNow,
            IsActive = true,
            IsPrimary = false
        };
        c.Assignments.Add(assignment);
        _unitOfWork.Add(assignment);
    }

    private static string GenerateTemporaryPassword()
    {
        // Mixed-case + digits + a symbol, so the generated value satisfies any
        // reasonable password policy without the recipient having to fix it up.
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
        var buffer = RandomNumberGenerator.GetBytes(10);
        var body = new string(buffer.Select(b => chars[b % chars.Length]).ToArray());
        return $"Ext@{body}1";
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
