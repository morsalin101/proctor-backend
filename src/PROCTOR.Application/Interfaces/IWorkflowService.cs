using PROCTOR.Domain.Enums;

namespace PROCTOR.Application.Interfaces;

public interface IWorkflowService
{
    Task<bool> ValidateTransitionAsync(CaseStatus from, CaseStatus to, string userRole, CaseType caseType);
    Task<CaseStatus?> GetForwardStatusAsync(string fromRole, string toRole, CaseStatus currentStatus, CaseType caseType);
}
