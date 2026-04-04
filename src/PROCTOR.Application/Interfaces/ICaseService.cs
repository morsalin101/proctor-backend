using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Cases;

namespace PROCTOR.Application.Interfaces;

public interface ICaseService
{
    Task<ApiResponse<PagedResult<CaseListDto>>> GetCasesAsync(string? status, string? type, string? priority, string? search, int page, int pageSize);
    Task<ApiResponse<CaseDto>> GetCaseByIdAsync(Guid id);
    Task<ApiResponse<CaseDto>> CreateCaseAsync(CreateCaseRequest request, string createdBy);
    Task<ApiResponse<CaseDto>> UpdateCaseAsync(Guid id, UpdateCaseRequest request);
    Task<ApiResponse<CaseDto>> UpdateCaseStatusAsync(Guid id, UpdateCaseStatusRequest request, string updatedBy);
}
