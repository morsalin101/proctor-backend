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

    public async Task<ApiResponse<List<HearingDto>>> GetHearingsAsync(Guid? caseId)
    {
        IEnumerable<Hearing> hearings;

        if (caseId.HasValue)
            hearings = await _unitOfWork.Hearings.GetByCaseIdAsync(caseId.Value);
        else
            hearings = await _unitOfWork.Hearings.GetAllAsync();

        var dtos = hearings.Select(h => h.ToDto()).ToList();
        return ApiResponse<List<HearingDto>>.SuccessResponse(dtos);
    }

    public async Task<ApiResponse<HearingDto>> GetHearingByIdAsync(Guid id)
    {
        var hearing = await _unitOfWork.Hearings.GetByIdWithCaseAsync(id);
        if (hearing is null)
            return ApiResponse<HearingDto>.FailResponse("Hearing not found.");

        return ApiResponse<HearingDto>.SuccessResponse(hearing.ToDto());
    }

    public async Task<ApiResponse<HearingDto>> CreateHearingAsync(CreateHearingRequest request)
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
            Status = HearingStatus.Scheduled
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

    public async Task<ApiResponse<HearingDto>> UpdateHearingStatusAsync(Guid id, string status)
    {
        var hearing = await _unitOfWork.Hearings.GetByIdWithCaseAsync(id);
        if (hearing is null)
            return ApiResponse<HearingDto>.FailResponse("Hearing not found.");

        var newStatus = MappingExtensions.ParseEnum<HearingStatus>(status);
        hearing.Status = newStatus;
        hearing.UpdatedAt = DateTime.UtcNow;

        if (newStatus == HearingStatus.Completed)
        {
            var existingCase = hearing.Case;
            existingCase.Status = CaseStatus.HearingCompleted;
            existingCase.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Cases.Update(existingCase);
        }

        _unitOfWork.Hearings.Update(hearing);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<HearingDto>.SuccessResponse(hearing.ToDto(), "Hearing status updated successfully.");
    }

    public async Task<ApiResponse<UpcomingHearingsDto>> GetUpcomingHearingsAsync(Guid? userId)
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
