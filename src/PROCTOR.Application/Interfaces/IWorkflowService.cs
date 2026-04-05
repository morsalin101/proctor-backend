using PROCTOR.Domain.Enums;

namespace PROCTOR.Application.Interfaces;

public interface IWorkflowService
{
    bool ValidateTransition(CaseStatus from, CaseStatus to, string userRole);
    CaseStatus? GetForwardStatus(string fromRole, string toRole, CaseStatus currentStatus);
}
