using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Ai;

namespace PROCTOR.Application.Interfaces;

public interface IAiService
{
    Task<ApiResponse<AiSettingsDto>> GetSettingsAsync();
    Task<ApiResponse<AiSettingsDto>> UpdateSettingsAsync(UpdateAiSettingsRequest request);

    /// <summary>Verifies that the key/model pair is accepted by the provider.</summary>
    Task<ApiResponse<AiTestResultDto>> TestConnectionAsync(TestAiConnectionRequest request);

    /// <summary>Drafts a full investigation report for a case in the requested language.</summary>
    Task<ApiResponse<GeneratedReportDto>> GenerateCaseReportAsync(Guid caseId, GenerateReportRequest request);
}
