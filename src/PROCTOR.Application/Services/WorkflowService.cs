using PROCTOR.Application.Interfaces;
using PROCTOR.Domain.Enums;

namespace PROCTOR.Application.Services;

public class WorkflowService : IWorkflowService
{
    private static readonly Dictionary<(CaseStatus From, CaseStatus To), HashSet<string>> Transitions = new()
    {
        // Coordinator / Female Coordinator verification
        { (CaseStatus.Submitted, CaseStatus.Verified), new() { "coordinator", "female-coordinator" } },
        { (CaseStatus.Submitted, CaseStatus.Rejected), new() { "coordinator", "female-coordinator" } },
        { (CaseStatus.Submitted, CaseStatus.OnHold), new() { "coordinator", "female-coordinator" } },
        { (CaseStatus.Submitted, CaseStatus.ResubmissionRequested), new() { "coordinator", "female-coordinator" } },
        { (CaseStatus.ResubmissionRequested, CaseStatus.Submitted), new() { "student" } },
        { (CaseStatus.ResubmissionRequested, CaseStatus.Verified), new() { "coordinator", "female-coordinator" } },
        { (CaseStatus.ResubmissionRequested, CaseStatus.Rejected), new() { "coordinator", "female-coordinator" } },

        // Coordinator forwards verified case (sets to Assigned)
        { (CaseStatus.Verified, CaseStatus.Assigned), new() { "coordinator", "female-coordinator", "proctor", "sexual-harassment-committee" } },

        // Proctor actions
        { (CaseStatus.Assigned, CaseStatus.Resolved), new() { "proctor", "sexual-harassment-committee" } },
        { (CaseStatus.Assigned, CaseStatus.PoliceCase), new() { "proctor", "sexual-harassment-committee" } },
        { (CaseStatus.Assigned, CaseStatus.ForwardedToRegistrar), new() { "proctor", "sexual-harassment-committee" } },

        // Assistant Proctor hearing workflow
        { (CaseStatus.Assigned, CaseStatus.HearingScheduled), new() { "assistant-proctor" } },
        { (CaseStatus.HearingScheduled, CaseStatus.HearingCompleted), new() { "assistant-proctor" } },

        // Deputy Proctor actions
        { (CaseStatus.HearingCompleted, CaseStatus.Assigned), new() { "deputy-proctor" } },
        { (CaseStatus.HearingCompleted, CaseStatus.Resolved), new() { "deputy-proctor" } },

        // Registrar actions
        { (CaseStatus.ForwardedToRegistrar, CaseStatus.ForwardedToCommittee), new() { "registrar" } },
        { (CaseStatus.ForwardedToRegistrar, CaseStatus.Assigned), new() { "registrar" } },

        // Disciplinary Committee actions
        { (CaseStatus.ForwardedToCommittee, CaseStatus.Closed), new() { "disciplinary-committee" } },
        { (CaseStatus.ForwardedToCommittee, CaseStatus.Assigned), new() { "disciplinary-committee" } },

        // SH Committee can close confidential cases
        { (CaseStatus.Assigned, CaseStatus.Closed), new() { "sexual-harassment-committee", "disciplinary-committee" } },

        // General close from resolved
        { (CaseStatus.Resolved, CaseStatus.Closed), new() { "proctor", "sexual-harassment-committee", "disciplinary-committee", "super-admin" } },

        // Police case is terminal (close it)
        { (CaseStatus.PoliceCase, CaseStatus.Closed), new() { "proctor", "sexual-harassment-committee", "super-admin" } },

        // OnHold can be resumed
        { (CaseStatus.OnHold, CaseStatus.Submitted), new() { "coordinator", "female-coordinator" } },
        { (CaseStatus.OnHold, CaseStatus.Verified), new() { "coordinator", "female-coordinator" } },
    };

    public bool ValidateTransition(CaseStatus from, CaseStatus to, string userRole)
    {
        // Super admin can do anything
        if (userRole == "super-admin")
            return true;

        if (Transitions.TryGetValue((from, to), out var allowedRoles))
            return allowedRoles.Contains(userRole);

        return false;
    }

    public CaseStatus? GetForwardStatus(string fromRole, string toRole, CaseStatus currentStatus)
    {
        return (fromRole, toRole) switch
        {
            // Coordinator forwards to proctor after verification
            ("coordinator" or "female-coordinator", "proctor") => CaseStatus.Assigned,
            ("coordinator" or "female-coordinator", "sexual-harassment-committee") => CaseStatus.Assigned,

            // Proctor assigns to subordinates
            ("proctor", "assistant-proctor" or "deputy-proctor") => CaseStatus.Assigned,

            // Assistant Proctor forwards to Deputy
            ("assistant-proctor", "deputy-proctor") => currentStatus,

            // Deputy Proctor actions
            ("deputy-proctor", "assistant-proctor") => CaseStatus.Assigned,
            ("deputy-proctor", "proctor") => CaseStatus.Assigned,

            // Proctor forwards to registrar
            ("proctor", "registrar") => CaseStatus.ForwardedToRegistrar,

            // Registrar actions
            ("registrar", "proctor") => CaseStatus.Assigned,
            ("registrar", "disciplinary-committee") => CaseStatus.ForwardedToCommittee,

            // SH Committee assigns to subordinates (confidential cases)
            ("sexual-harassment-committee", "assistant-proctor" or "deputy-proctor") => CaseStatus.Assigned,
            ("sexual-harassment-committee", "registrar") => CaseStatus.ForwardedToRegistrar,

            // Disciplinary Committee returns to proctor
            ("disciplinary-committee", "proctor") => CaseStatus.Assigned,

            _ => null
        };
    }
}
