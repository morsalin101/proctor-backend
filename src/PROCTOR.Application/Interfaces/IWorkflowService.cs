using PROCTOR.Domain.Enums;

namespace PROCTOR.Application.Interfaces;

public interface IWorkflowService
{
    bool ValidateTransition(CaseStatus from, CaseStatus to, string userRole);
    Task<CaseStatus?> GetForwardStatusAsync(string fromRole, string toRole, CaseStatus currentStatus);
}
