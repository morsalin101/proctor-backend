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

    public HearingService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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
        var existingCase = await _unitOfWork.Cases.GetByIdAsync(caseId);
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

        // Update case status to HearingScheduled
        existingCase.Status = CaseStatus.HearingScheduled;
        existingCase.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Cases.Update(existingCase);

        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<HearingDto>.SuccessResponse(hearing.ToDto(), "Hearing created successfully.");
    }

    public async Task<ApiResponse<HearingDto>> UpdateHearingAsync(Guid id, UpdateHearingRequest request)
    {
        var hearing = await _unitOfWork.Hearings.GetByIdAsync(id);
        if (hearing is null)
            return ApiResponse<HearingDto>.FailResponse("Hearing not found.");

        if (request.Date is not null)
            hearing.Date = request.Date;

        if (request.Time is not null)
            hearing.Time = request.Time;

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

        // If hearing completed, update case status
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
}
