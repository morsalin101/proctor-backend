using System.Globalization;
using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Hearings;
using PROCTOR.Application.Interfaces;
using PROCTOR.Application.Mapping;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Domain.Interfaces;

namespace PROCTOR.Application.Services;

public class HearingService : IHearingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;

    public HearingService(IUnitOfWork unitOfWork, INotificationService notificationService, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _emailService = emailService;
    }

    // Female officer visibility includes female complaints and restricted cases.
    private static bool IsFemaleTrack(Case? c) =>
        c is not null && (c.IsConfidential || c.Type == CaseType.Confidential || c.SubmitterGender == Gender.Female);

    // The Administrative Officer ("coordinator") sees every hearing, matching the Proctor.
    // Only the Female Coordinator stays scoped to her own track.
    private static bool CoordinatorMayView(string? role, Case? c)
    {
        // Mirrors CaseService: her own track, plus every instant (Type-1) incident.
        if (role == "female-coordinator") return IsFemaleTrack(c) || c?.Type == CaseType.Type1;
        return true;
    }

    public async Task<ApiResponse<List<HearingDto>>> GetHearingsAsync(Guid? caseId, string? userRole = null)
    {
        IEnumerable<Hearing> hearings;

        if (caseId.HasValue)
            hearings = await _unitOfWork.Hearings.GetByCaseIdAsync(caseId.Value);
        else
            hearings = await _unitOfWork.Hearings.GetAllWithCaseAsync();

        // Hide female-track hearings from the male Coordinator (and vice-versa).
        var dtos = hearings
            .Where(h => CoordinatorMayView(userRole, h.Case))
            .Select(h => h.ToDto())
            .ToList();
        return ApiResponse<List<HearingDto>>.SuccessResponse(dtos);
    }

    public async Task<ApiResponse<HearingDto>> GetHearingByIdAsync(Guid id)
    {
        var hearing = await _unitOfWork.Hearings.GetByIdWithCaseAsync(id);
        if (hearing is null)
            return ApiResponse<HearingDto>.FailResponse("Hearing not found.");

        return ApiResponse<HearingDto>.SuccessResponse(hearing.ToDto());
    }

    public async Task<ApiResponse<HearingDto>> CreateHearingAsync(CreateHearingRequest request, Guid? createdById = null, string? createdByName = null)
    {
        var caseId = Guid.Parse(request.CaseId);
        var existingCase = await _unitOfWork.Cases.GetByIdWithDetailsAsync(caseId);
        if (existingCase is null)
            return ApiResponse<HearingDto>.FailResponse("Case not found.");

        var hearing = new Hearing
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            Date = request.Date,
            Time = request.Time,
            Location = request.Location,
            Participants = request.Participants,
            Status = HearingStatus.Scheduled,
            CreatedById = createdById,
            CreatedByName = createdByName
        };

        await _unitOfWork.Hearings.AddAsync(hearing);

        existingCase.Status = CaseStatus.HearingScheduled;
        existingCase.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Cases.Update(existingCase);

        await _unitOfWork.SaveChangesAsync();

        await NotifyHearingChangedAsync(existingCase, hearing, "Hearing Scheduled");

        // Reload with case so DTO includes case info
        var reloaded = await _unitOfWork.Hearings.GetByIdWithCaseAsync(hearing.Id);
        return ApiResponse<HearingDto>.SuccessResponse((reloaded ?? hearing).ToDto(), "Hearing created successfully.");
    }

    public async Task<ApiResponse<HearingDto>> UpdateHearingAsync(Guid id, UpdateHearingRequest request)
    {
        var hearing = await _unitOfWork.Hearings.GetByIdWithCaseAsync(id);
        if (hearing is null)
            return ApiResponse<HearingDto>.FailResponse("Hearing not found.");

        var dateChanged = false;

        if (request.Date is not null && request.Date != hearing.Date)
        {
            hearing.Date = request.Date;
            dateChanged = true;
        }

        if (request.Time is not null && request.Time != hearing.Time)
        {
            hearing.Time = request.Time;
            dateChanged = true;
        }

        if (request.Location is not null)
            hearing.Location = request.Location;

        if (request.Participants is not null)
            hearing.Participants = request.Participants;

        if (request.Notes is not null)
            hearing.Notes = request.Notes;

        if (request.Remarks is not null)
            hearing.Remarks = request.Remarks;

        hearing.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Hearings.Update(hearing);
        await _unitOfWork.SaveChangesAsync();

        if (dateChanged && hearing.Case is not null)
        {
            var fullCase = await _unitOfWork.Cases.GetByIdWithDetailsAsync(hearing.CaseId);
            if (fullCase is not null)
                await NotifyHearingChangedAsync(fullCase, hearing, "Hearing Rescheduled");
        }

        return ApiResponse<HearingDto>.SuccessResponse(hearing.ToDto(), "Hearing updated successfully.");
    }

    public async Task<ApiResponse<HearingDto>> UpdateHearingStatusAsync(Guid id, string status, Guid? actingUserId = null, string? actingUserName = null)
    {
        var hearing = await _unitOfWork.Hearings.GetByIdWithCaseAsync(id);
        if (hearing is null)
            return ApiResponse<HearingDto>.FailResponse("Hearing not found.");

        var newStatus = MappingExtensions.ParseEnum<HearingStatus>(status);

        // Closing (completing) a hearing is reserved for the user who set it, so the same
        // person who opened the hearing is the one who closes it out with remarks. Legacy
        // hearings without a recorded creator (CreatedById == null) are left unrestricted.
        if (newStatus == HearingStatus.Completed
            && hearing.CreatedById.HasValue
            && actingUserId.HasValue
            && hearing.CreatedById.Value != actingUserId.Value)
        {
            return ApiResponse<HearingDto>.FailResponse("Only the person who set this hearing can close it.");
        }

        hearing.Status = newStatus;
        hearing.UpdatedAt = DateTime.UtcNow;

        if (newStatus == HearingStatus.Completed)
        {
            // Record who actually conducted the hearing — the user closing it out is the
            // one who chaired it, since only the setter may complete a hearing.
            hearing.ConductedById = actingUserId ?? hearing.CreatedById;
            hearing.ConductedByName = actingUserName ?? hearing.CreatedByName;
            hearing.ConductedAt = DateTime.UtcNow;

            var existingCase = hearing.Case;
            existingCase.Status = CaseStatus.HearingCompleted;
            existingCase.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Cases.Update(existingCase);
        }

        _unitOfWork.Hearings.Update(hearing);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<HearingDto>.SuccessResponse(hearing.ToDto(), "Hearing status updated successfully.");
    }

    public async Task<ApiResponse<HearingDto>> RescheduleHearingAsync(
        Guid id, RescheduleHearingRequest request, Guid? actingUserId, string actingUserName)
    {
        var hearing = await _unitOfWork.Hearings.GetByIdWithCaseAsync(id);
        if (hearing is null)
            return ApiResponse<HearingDto>.FailResponse("Hearing not found.");

        if (hearing.Status != HearingStatus.Scheduled)
            return ApiResponse<HearingDto>.FailResponse("Only a scheduled hearing can be rescheduled.");

        var date = (request.Date ?? string.Empty).Trim();
        var time = (request.Time ?? string.Empty).Trim();
        var reason = (request.Reason ?? string.Empty).Trim();
        if (date.Length == 0 || time.Length == 0)
            return ApiResponse<HearingDto>.FailResponse("A new date and time are required.");
        if (reason.Length == 0)
            return ApiResponse<HearingDto>.FailResponse("A reason for rescheduling is required.");

        // Only the person who set the hearing may move it, matching who may close it.
        if (hearing.CreatedById.HasValue && actingUserId.HasValue && hearing.CreatedById.Value != actingUserId.Value)
            return ApiResponse<HearingDto>.FailResponse("Only the person who set this hearing can reschedule it.");

        var location = string.IsNullOrWhiteSpace(request.Location) ? hearing.Location : request.Location!.Trim();
        if (date == hearing.Date && time == hearing.Time && location == hearing.Location)
            return ApiResponse<HearingDto>.FailResponse("The new slot is the same as the current one.");

        hearing.Reschedules = new List<HearingReschedule>(hearing.Reschedules)
        {
            new()
            {
                Id = Guid.NewGuid().ToString(),
                FromDate = hearing.Date,
                FromTime = hearing.Time,
                FromLocation = hearing.Location,
                ToDate = date,
                ToTime = time,
                ToLocation = location,
                Reason = reason,
                RescheduledBy = actingUserName,
                RescheduledAt = DateTime.UtcNow
            }
        };

        var previous = $"{hearing.Date} at {hearing.Time}";
        hearing.Date = date;
        hearing.Time = time;
        hearing.Location = location;
        hearing.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Hearings.Update(hearing);

        _unitOfWork.Add(new TimelineEvent
        {
            Id = Guid.NewGuid(),
            CaseId = hearing.CaseId,
            Action = "Hearing Rescheduled",
            Description = $"Hearing moved from {previous} to {date} at {time} ({location}). Reason: \"{reason}\"",
            User = actingUserName
        });

        await _unitOfWork.SaveChangesAsync();

        var c = hearing.Case;
        if (c?.SubmittedByUserId is not null)
        {
            await _notificationService.CreateAsync(c.SubmittedByUserId.Value, null,
                "Hearing Rescheduled",
                $"The hearing for case {c.CaseNumber} has moved to {date} at {time} ({location}). Reason: \"{reason}\"",
                c.Id);
        }

        return ApiResponse<HearingDto>.SuccessResponse(hearing.ToDto(), "Hearing rescheduled.");
    }

    public async Task<ApiResponse<UpcomingHearingsDto>> GetUpcomingHearingsAsync(Guid? userId, string? userRole = null)
    {
        var allHearings = await _unitOfWork.Hearings.GetAllAsync();
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var endOfWeek = today.AddDays(7);

        var result = new UpcomingHearingsDto();

        foreach (var h in allHearings)
        {
            if (h.Status == HearingStatus.Completed || h.Status == HearingStatus.Cancelled)
                continue;

            if (!DateTime.TryParse(h.Date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var hDate))
                continue;

            // Filter to hearings on cases assigned to the user (if userId provided)
            if (userId.HasValue)
            {
                var c = await _unitOfWork.Cases.GetByIdWithDetailsAsync(h.CaseId);
                if (c is null) continue;
                var participates = c.AssignedToId == userId.Value
                    || c.Assignments.Any(a => a.IsActive && a.UserId == userId.Value);
                if (!participates) continue;
            }

            var hearingWithCase = await _unitOfWork.Hearings.GetByIdWithCaseAsync(h.Id);

            // Keep female-track hearings away from the male Coordinator (and vice-versa).
            if (!CoordinatorMayView(userRole, hearingWithCase?.Case))
                continue;

            var dto = (hearingWithCase ?? h).ToDto();

            var dateOnly = hDate.Date;
            if (dateOnly < today)
            {
                continue;
            }
            else if (dateOnly == today)
            {
                result.Today.Add(dto);
            }
            else if (dateOnly == tomorrow)
            {
                result.Tomorrow.Add(dto);
            }
            else if (dateOnly <= endOfWeek)
            {
                result.ThisWeek.Add(dto);
            }
            else
            {
                result.Later.Add(dto);
            }
        }

        return ApiResponse<UpcomingHearingsDto>.SuccessResponse(result);
    }

    public async Task<ApiResponse<HearingDto>> SendHearingEmailAsync(Guid id, NotifyHearingEmailRequest request, string sentByName)
    {
        var hearing = await _unitOfWork.Hearings.GetByIdWithCaseAsync(id);
        if (hearing is null)
            return ApiResponse<HearingDto>.FailResponse("Hearing not found.");

        var recipients = (request.Recipients ?? new List<string>())
            .Select(r => r?.Trim() ?? string.Empty)
            .Where(r => r.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (recipients.Count == 0)
            return ApiResponse<HearingDto>.FailResponse("At least one recipient email is required.");
        if (string.IsNullOrWhiteSpace(request.Message))
            return ApiResponse<HearingDto>.FailResponse("A message is required.");

        var caseNumber = hearing.Case?.CaseNumber ?? "";
        var subject = string.IsNullOrWhiteSpace(request.Subject)
            ? $"Hearing notification — {caseNumber}".Trim()
            : request.Subject!.Trim();
        var body = $"{request.Message}\n\nHearing: {hearing.Date} at {hearing.Time}, {hearing.Location}\n\n" +
                   "This is a demo email — no real SMTP is configured yet.";

        // Dummy send (logs + records a SentEmail row); no real SMTP configured.
        foreach (var to in recipients)
            await _emailService.SendAsync(to, subject, body, hearing.CaseId);

        // Append to the hearing's notification log so the UI can show who was notified.
        hearing.EmailNotifications = new List<HearingEmailNotification>(hearing.EmailNotifications)
        {
            new HearingEmailNotification
            {
                Recipients = recipients,
                Subject = subject,
                Message = request.Message.Trim(),
                SentBy = sentByName,
                SentAt = DateTime.UtcNow
            }
        };
        hearing.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Hearings.Update(hearing);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<HearingDto>.SuccessResponse(hearing.ToDto(), $"Email sent to {recipients.Count} recipient(s).");
    }

    private async Task NotifyHearingChangedAsync(Case c, Hearing hearing, string title)
    {
        var message = $"Hearing for case {c.CaseNumber} is on {hearing.Date} at {hearing.Time} ({hearing.Location}).";

        var notifyUserIds = new HashSet<Guid>();
        if (c.AssignedToId.HasValue) notifyUserIds.Add(c.AssignedToId.Value);
        foreach (var a in c.Assignments.Where(a => a.IsActive))
            notifyUserIds.Add(a.UserId);
        if (c.SubmittedByUserId.HasValue) notifyUserIds.Add(c.SubmittedByUserId.Value);

        foreach (var uid in notifyUserIds)
        {
            await _notificationService.CreateAsync(uid, null, title, message, c.Id);
            var user = await _unitOfWork.Users.GetByIdAsync(uid);
            if (user is not null && !string.IsNullOrWhiteSpace(user.Email))
            {
                var body = $"Hello {user.Name},\n\n{message}\n\nThis is a demo email — no real SMTP is configured yet.";
                await _emailService.SendAsync(user.Email, title + ": " + c.CaseNumber, body, c.Id);
            }
        }
    }
}
