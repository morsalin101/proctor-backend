using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Hearings;

namespace PROCTOR.Application.Interfaces;

public interface IHearingService
{
    Task<ApiResponse<List<HearingDto>>> GetHearingsAsync(Guid? caseId);
    Task<ApiResponse<HearingDto>> GetHearingByIdAsync(Guid id);
    Task<ApiResponse<HearingDto>> CreateHearingAsync(CreateHearingRequest request);
    Task<ApiResponse<HearingDto>> UpdateHearingAsync(Guid id, UpdateHearingRequest request);
    Task<ApiResponse<HearingDto>> UpdateHearingStatusAsync(Guid id, string status);
}
