using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.ForwardingRules;

namespace PROCTOR.Application.Interfaces;

public interface IForwardingRuleService
{
    Task<ApiResponse<List<ForwardingRuleDto>>> GetAllAsync();
    Task<ApiResponse<List<ForwardingRuleDto>>> GetRulesForRoleAsync(string fromRole, string caseType = "type-2");
    Task<ApiResponse<SpecialPermissionDto>> GetSpecialPermissionsAsync(string fromRole, string caseType = "type-2");
    Task<ApiResponse<ForwardingRuleDto>> CreateAsync(CreateForwardingRuleRequest request);
    Task<ApiResponse<ForwardingRuleDto>> UpdateAsync(Guid id, UpdateForwardingRuleRequest request);
    Task<ApiResponse<bool>> DeleteAsync(Guid id);
}
