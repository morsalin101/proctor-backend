using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Cases;
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

    public CaseService(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
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

    public async Task<ApiResponse<CaseDto>> CreateCaseAsync(CreateCaseRequest request, string createdBy)
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
            Description = request.Description
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

        // Notify relevant roles
        await _notificationService.CreateAsync(null, "proctor", "New Case Submitted",
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

    public async Task<ApiResponse<CaseDto>> UpdateCaseStatusAsync(Guid id, UpdateCaseStatusRequest request, string updatedBy)
    {
        var c = await _unitOfWork.Cases.GetByIdWithDetailsAsync(id);
        if (c is null)
            return ApiResponse<CaseDto>.FailResponse("Case not found.");

        var oldStatus = c.Status;
        var newStatus = MappingExtensions.ParseEnum<CaseStatus>(request.Status);
        c.Status = newStatus;

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

        // Notify about status change
        await _notificationService.CreateAsync(null, "proctor",
            "Case Status Updated", $"Case {c.CaseNumber} status changed to {newStatus.ToKebabCase()}.", c.Id);

        var updated = await _unitOfWork.Cases.GetByIdWithDetailsAsync(id);
        return ApiResponse<CaseDto>.SuccessResponse(updated!.ToDto(), "Case status updated successfully.");
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
}
