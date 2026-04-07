using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Cases;
using PROCTOR.Application.DTOs.Reports;

namespace PROCTOR.Application.Interfaces;

public interface ICaseService
{
    Task<ApiResponse<PagedResult<CaseListDto>>> GetCasesAsync(string? status, string? type, string? priority, string? search, int page, int pageSize);
    Task<ApiResponse<CaseDto>> GetCaseByIdAsync(Guid id);
    Task<ApiResponse<CaseDto>> CreateCaseAsync(CreateCaseRequest request, string createdBy, Guid? submittedByUserId = null);
    Task<ApiResponse<CaseDto>> UpdateCaseAsync(Guid id, UpdateCaseRequest request);
    Task<ApiResponse<CaseDto>> UpdateCaseStatusAsync(Guid id, UpdateCaseStatusRequest request, string updatedBy, string userRole);
    Task<ApiResponse<CaseDto>> ForwardCaseAsync(Guid id, ForwardCaseRequest request, string updatedBy, string userRole);
    Task<ApiResponse<ReportDto>> CreateReportAsync(Guid caseId, CreateReportRequest request, string createdByName, Guid createdById);
    Task<ApiResponse<List<ReportDto>>> GetReportsAsync(Guid caseId);
    Task<ApiResponse<ReportDto>> UpdateReportAsync(Guid caseId, Guid reportId, CreateReportRequest request, string updatedByName);
    Task<ApiResponse<bool>> DeleteCaseAsync(Guid id);
    Task<ApiResponse<PagedResult<CaseListDto>>> GetMyCasesAsync(Guid userId, string userRole, int page, int pageSize);
    Task<ApiResponse<int>> GetMyCasesCountAsync(Guid userId, string userRole);
}
