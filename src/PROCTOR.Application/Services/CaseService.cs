using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Cases;
using PROCTOR.Application.DTOs.Reports;
using PROCTOR.Application.Interfaces;
using PROCTOR.Application.Mapping;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Domain.Interfaces;

namespace PROCTOR.Application.Services;

public class CaseService : ICaseService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IWorkflowService _workflowService;

    public CaseService(IUnitOfWork unitOfWork, INotificationService notificationService, IWorkflowService workflowService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _workflowService = workflowService;
    }

    public async Task<ApiResponse<PagedResult<CaseListDto>>> GetCasesAsync(
        string? status, string? type, string? priority, string? search, int page, int pageSize)
    {
        CaseStatus? statusEnum = status is not null ? MappingExtensions.ParseEnum<CaseStatus>(status) : null;
        CaseType? typeEnum = type is not null ? MappingExtensions.ParseEnum<CaseType>(type) : null;
        Priority? priorityEnum = priority is not null ? MappingExtensions.ParseEnum<Priority>(priority) : null;

        var cases = await _unitOfWork.Cases.GetFilteredAsync(statusEnum, typeEnum, priorityEnum, search, page, pageSize);
        var totalCount = await _unitOfWork.Cases.GetFilteredCountAsync(statusEnum, typeEnum, priorityEnum, search);

        var result = new PagedResult<CaseListDto>
        {
            Items = cases.Select(c => c.ToListDto()),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return ApiResponse<PagedResult<CaseListDto>>.SuccessResponse(result);
    }

    public async Task<ApiResponse<CaseDto>> GetCaseByIdAsync(Guid id)
    {
        var c = await _unitOfWork.Cases.GetByIdWithDetailsAsync(id);
        if (c is null)
            return ApiResponse<CaseDto>.FailResponse("Case not found.");

        return ApiResponse<CaseDto>.SuccessResponse(c.ToDto());
    }

    public async Task<ApiResponse<CaseDto>> CreateCaseAsync(CreateCaseRequest request, string createdBy, Guid? submittedByUserId = null)
    {
        var caseNumber = await _unitOfWork.Cases.GenerateCaseNumberAsync();

        var newCase = new Case
        {
            Id = Guid.NewGuid(),
            CaseNumber = caseNumber,
            StudentName = request.StudentName,
            StudentId = request.StudentId,
            Type = MappingExtensions.ParseEnum<CaseType>(request.Type),
            Status = CaseStatus.Submitted,
            Priority = MappingExtensions.ParseEnum<Priority>(request.Priority),
            Description = request.Description,
            // Type-2 form fields
            StudentDepartment = request.StudentDepartment,
            StudentContact = request.StudentContact,
            StudentAdvisorName = request.StudentAdvisorName,
            StudentFatherName = request.StudentFatherName,
            StudentFatherContact = request.StudentFatherContact,
            AccusedName = request.AccusedName,
            AccusedId = request.AccusedId,
            AccusedDepartment = request.AccusedDepartment,
            AccusedContact = request.AccusedContact,
            AccusedGuardianContact = request.AccusedGuardianContact,
            VideoLink = request.VideoLink,
            IncidentDate = request.IncidentDate is not null ? DateTime.SpecifyKind(DateTime.Parse(request.IncidentDate), DateTimeKind.Utc) : null,
            SubmittedByUserId = submittedByUserId
        };

        var timelineEvent = new TimelineEvent
        {
            Id = Guid.NewGuid(),
            CaseId = newCase.Id,
            Action = "Case Created",
            Description = $"Case {caseNumber} was created.",
            User = createdBy
        };

        newCase.TimelineEvents.Add(timelineEvent);

        await _unitOfWork.Cases.AddAsync(newCase);
        await _unitOfWork.SaveChangesAsync();

        // Reload with details
        var created = await _unitOfWork.Cases.GetByIdWithDetailsAsync(newCase.Id);

        // Notify relevant roles based on case type
        var targetRole = newCase.Type == CaseType.Confidential ? "female-coordinator" : "coordinator";
        await _notificationService.CreateAsync(null, targetRole, "New Case Submitted",
            $"Case {caseNumber} has been submitted by {request.StudentName}.", newCase.Id);

        return ApiResponse<CaseDto>.SuccessResponse(created!.ToDto(), "Case created successfully.");
    }

    public async Task<ApiResponse<CaseDto>> UpdateCaseAsync(Guid id, UpdateCaseRequest request)
    {
        var c = await _unitOfWork.Cases.GetByIdWithDetailsAsync(id);
        if (c is null)
            return ApiResponse<CaseDto>.FailResponse("Case not found.");

        if (request.Priority is not null)
            c.Priority = MappingExtensions.ParseEnum<Priority>(request.Priority);

        if (request.Description is not null)
            c.Description = request.Description;

        if (request.AssignedToId is not null)
        {
            c.AssignedToId = Guid.Parse(request.AssignedToId);
            c.Status = CaseStatus.Assigned;
        }

        // Type-2 form fields update
        if (request.StudentDepartment is not null) c.StudentDepartment = request.StudentDepartment;
        if (request.StudentContact is not null) c.StudentContact = request.StudentContact;
        if (request.StudentAdvisorName is not null) c.StudentAdvisorName = request.StudentAdvisorName;
        if (request.StudentFatherName is not null) c.StudentFatherName = request.StudentFatherName;
        if (request.StudentFatherContact is not null) c.StudentFatherContact = request.StudentFatherContact;
        if (request.AccusedName is not null) c.AccusedName = request.AccusedName;
        if (request.AccusedId is not null) c.AccusedId = request.AccusedId;
        if (request.AccusedDepartment is not null) c.AccusedDepartment = request.AccusedDepartment;
        if (request.AccusedContact is not null) c.AccusedContact = request.AccusedContact;
        if (request.AccusedGuardianContact is not null) c.AccusedGuardianContact = request.AccusedGuardianContact;
        if (request.VideoLink is not null) c.VideoLink = request.VideoLink;
        if (request.IncidentDate is not null) c.IncidentDate = DateTime.Parse(request.IncidentDate).ToUniversalTime();

        _unitOfWork.Cases.Update(c);

        _unitOfWork.Add(new TimelineEvent
        {
            Id = Guid.NewGuid(),
            CaseId = c.Id,
            Action = "Case Updated",
            Description = "Case details were updated.",
            User = "System"
        });

        await _unitOfWork.SaveChangesAsync();

        if (request.AssignedToId is not null)
        {
            await _notificationService.CreateAsync(Guid.Parse(request.AssignedToId), null,
                "Case Assigned", $"You have been assigned to case {c.CaseNumber}.", c.Id);
        }

        var updated = await _unitOfWork.Cases.GetByIdWithDetailsAsync(id);
        return ApiResponse<CaseDto>.SuccessResponse(updated!.ToDto(), "Case updated successfully.");
    }

    public async Task<ApiResponse<CaseDto>> UpdateCaseStatusAsync(Guid id, UpdateCaseStatusRequest request, string updatedBy, string userRole)
    {
        var c = await _unitOfWork.Cases.GetByIdWithDetailsAsync(id);
        if (c is null)
            return ApiResponse<CaseDto>.FailResponse("Case not found.");

        var oldStatus = c.Status;
        var newStatus = MappingExtensions.ParseEnum<CaseStatus>(request.Status);

        if (!_workflowService.ValidateTransition(oldStatus, newStatus, userRole))
            return ApiResponse<CaseDto>.FailResponse($"Transition from '{oldStatus.ToKebabCase()}' to '{newStatus.ToKebabCase()}' is not allowed for role '{userRole}'.");

        c.Status = newStatus;

        if (!string.IsNullOrWhiteSpace(request.Verdict))
            c.Verdict = request.Verdict;

        if (!string.IsNullOrWhiteSpace(request.Recommendation))
            c.Recommendation = request.Recommendation;

        _unitOfWork.Cases.Update(c);

        _unitOfWork.Add(new TimelineEvent
        {
            Id = Guid.NewGuid(),
            CaseId = c.Id,
            Action = "Status Changed",
            Description = $"Status changed from {oldStatus.ToKebabCase()} to {newStatus.ToKebabCase()}.",
            User = updatedBy
        });

        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            _unitOfWork.Add(new Note
            {
                Id = Guid.NewGuid(),
                CaseId = c.Id,
                Content = request.Note,
                Author = updatedBy
            });
        }

        await _unitOfWork.SaveChangesAsync();

        // Send role-appropriate notifications
        if (newStatus == CaseStatus.ResubmissionRequested && c.SubmittedByUserId.HasValue)
        {
            // Notify the student who submitted the case
            await _notificationService.CreateAsync(c.SubmittedByUserId.Value, "student",
                "Resubmission Requested", $"Case {c.CaseNumber} needs changes. Please review the coordinator comments and resubmit.", c.Id);
        }
        else if (newStatus == CaseStatus.Rejected && c.SubmittedByUserId.HasValue)
        {
            await _notificationService.CreateAsync(c.SubmittedByUserId.Value, "student",
                "Case Rejected", $"Case {c.CaseNumber} has been rejected.", c.Id);
        }
        else if (newStatus == CaseStatus.Submitted)
        {
            // Notify coordinator when student resubmits
            var targetRole = c.Type == CaseType.Confidential ? "female-coordinator" : "coordinator";
            await _notificationService.CreateAsync(null, targetRole,
                "Case Resubmitted", $"Case {c.CaseNumber} has been resubmitted by {c.StudentName}.", c.Id);
        }
        else
        {
            var notifyRole = newStatus switch
            {
                CaseStatus.Verified => "proctor",
                CaseStatus.ForwardedToRegistrar => "registrar",
                CaseStatus.ForwardedToCommittee => "disciplinary-committee",
                CaseStatus.Resolved or CaseStatus.Closed => "proctor",
                _ => "proctor"
            };

            await _notificationService.CreateAsync(null, notifyRole,
                "Case Status Updated", $"Case {c.CaseNumber} status changed to {newStatus.ToKebabCase()}.", c.Id);
        }

        // Also notify the student on every status change
        if (newStatus != CaseStatus.ResubmissionRequested && newStatus != CaseStatus.Rejected && newStatus != CaseStatus.Submitted && c.SubmittedByUserId.HasValue)
        {
            await _notificationService.CreateAsync(c.SubmittedByUserId.Value, null,
                "Case Update", $"Your case {c.CaseNumber} status changed to {newStatus.ToKebabCase()}.", c.Id);
        }

        var updated = await _unitOfWork.Cases.GetByIdWithDetailsAsync(id);
        return ApiResponse<CaseDto>.SuccessResponse(updated!.ToDto(), "Case status updated successfully.");
    }

    public async Task<ApiResponse<CaseDto>> ForwardCaseAsync(Guid id, ForwardCaseRequest request, string updatedBy, string userRole)
    {
        var c = await _unitOfWork.Cases.GetByIdWithDetailsAsync(id);
        if (c is null)
            return ApiResponse<CaseDto>.FailResponse("Case not found.");

        var newStatus = _workflowService.GetForwardStatus(userRole, request.TargetRole, c.Status);
        if (newStatus is null)
            return ApiResponse<CaseDto>.FailResponse($"Cannot forward case from role '{userRole}' to '{request.TargetRole}'.");

        var oldStatus = c.Status;
        c.Status = newStatus.Value;
        c.ForwardedToRole = request.TargetRole;

        // Individual assignment
        if (!string.IsNullOrWhiteSpace(request.AssignedToUserId) && Guid.TryParse(request.AssignedToUserId, out var assigneeId))
            c.AssignedToId = assigneeId;

        if (!string.IsNullOrWhiteSpace(request.Recommendation))
            c.Recommendation = request.Recommendation;

        if (!string.IsNullOrWhiteSpace(request.Verdict))
            c.Verdict = request.Verdict;

        _unitOfWork.Cases.Update(c);

        _unitOfWork.Add(new TimelineEvent
        {
            Id = Guid.NewGuid(),
            CaseId = c.Id,
            Action = "Case Forwarded",
            Description = $"Case forwarded from {userRole} to {request.TargetRole}. Status changed from {oldStatus.ToKebabCase()} to {newStatus.Value.ToKebabCase()}.",
            User = updatedBy
        });

        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            _unitOfWork.Add(new Note
            {
                Id = Guid.NewGuid(),
                CaseId = c.Id,
                Content = request.Note,
                Author = updatedBy
            });
        }

        await _unitOfWork.SaveChangesAsync();

        // Always notify the target role so all users of that role see it
        await _notificationService.CreateAsync(null, request.TargetRole,
            "Case Forwarded to You", $"Case {c.CaseNumber} has been forwarded to your attention by {updatedBy}.", c.Id);

        // Additionally notify the specific assigned user if individually assigned
        if (!string.IsNullOrWhiteSpace(request.AssignedToUserId) && Guid.TryParse(request.AssignedToUserId, out var notifyUserId))
        {
            await _notificationService.CreateAsync(notifyUserId, null,
                "Case Assigned to You", $"Case {c.CaseNumber} has been specifically assigned to you.", c.Id);
        }

        var updated = await _unitOfWork.Cases.GetByIdWithDetailsAsync(id);
        return ApiResponse<CaseDto>.SuccessResponse(updated!.ToDto(), "Case forwarded successfully.");
    }

    public async Task<ApiResponse<ReportDto>> CreateReportAsync(Guid caseId, CreateReportRequest request, string createdByName, Guid createdById)
    {
        var c = await _unitOfWork.Cases.GetByIdWithDetailsAsync(caseId);
        if (c is null)
            return ApiResponse<ReportDto>.FailResponse("Case not found.");

        var report = new Report
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            Content = request.Content,
            CreatedByName = createdByName,
            CreatedById = createdById,
            IsDraft = request.IsDraft,
            IsFinal = request.IsFinal
        };

        _unitOfWork.Add(report);

        _unitOfWork.Add(new TimelineEvent
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            Action = request.IsDraft ? "Draft Report Created" : "Report Submitted",
            Description = $"{(request.IsDraft ? "Draft report" : "Report")} created by {createdByName}.",
            User = createdByName
        });

        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<ReportDto>.SuccessResponse(report.ToDto(), "Report created successfully.");
    }

    public async Task<ApiResponse<List<ReportDto>>> GetReportsAsync(Guid caseId)
    {
        var c = await _unitOfWork.Cases.GetByIdWithDetailsAsync(caseId);
        if (c is null)
            return ApiResponse<List<ReportDto>>.FailResponse("Case not found.");

        var reports = c.Reports.Select(r => r.ToDto()).ToList();
        return ApiResponse<List<ReportDto>>.SuccessResponse(reports);
    }

    public async Task<ApiResponse<bool>> DeleteCaseAsync(Guid id)
    {
        var c = await _unitOfWork.Cases.GetByIdWithDetailsAsync(id);
        if (c is null)
            return ApiResponse<bool>.FailResponse("Case not found.");

        _unitOfWork.Cases.Remove(c);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true, "Case deleted successfully.");
    }

    public async Task<ApiResponse<PagedResult<CaseListDto>>> GetMyCasesAsync(Guid userId, string userRole, int page, int pageSize)
    {
        var allCases = await _unitOfWork.Cases.FindAsync(c =>
            c.ForwardedToRole == userRole || c.AssignedToId == userId || c.SubmittedByUserId == userId);

        var totalCount = allCases.Count();
        var items = allCases
            .OrderByDescending(c => c.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => c.ToListDto());

        var result = new PagedResult<CaseListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return ApiResponse<PagedResult<CaseListDto>>.SuccessResponse(result);
    }

    public async Task<ApiResponse<int>> GetMyCasesCountAsync(Guid userId, string userRole)
    {
        var count = await _unitOfWork.Cases.CountAsync(c =>
            c.ForwardedToRole == userRole || c.AssignedToId == userId || c.SubmittedByUserId == userId);

        return ApiResponse<int>.SuccessResponse(count);
    }
}
