using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Hearings;

namespace PROCTOR.Application.Interfaces;

public interface IHearingService
{
    Task<ApiResponse<List<HearingDto>>> GetHearingsAsync(Guid? caseId, string? userRole = null);
    Task<ApiResponse<HearingDto>> GetHearingByIdAsync(Guid id);
    Task<ApiResponse<HearingDto>> CreateHearingAsync(CreateHearingRequest request, Guid? createdById = null, string? createdByName = null);
    Task<ApiResponse<HearingDto>> UpdateHearingAsync(Guid id, UpdateHearingRequest request);
    Task<ApiResponse<HearingDto>> UpdateHearingStatusAsync(Guid id, string status, Guid? actingUserId = null);
    Task<ApiResponse<UpcomingHearingsDto>> GetUpcomingHearingsAsync(Guid? userId, string? userRole = null);
    Task<ApiResponse<HearingDto>> SendHearingEmailAsync(Guid id, NotifyHearingEmailRequest request, string sentByName);
}
