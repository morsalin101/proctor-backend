using PROCTOR.Application.Interfaces;
using PROCTOR.Application.Mapping;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Domain.Interfaces;

namespace PROCTOR.Application.Services;

public class WorkflowService : IWorkflowService
{
    private readonly IRepository<ForwardingRule> _forwardingRuleRepository;

    public WorkflowService(IRepository<ForwardingRule> forwardingRuleRepository)
    {
        _forwardingRuleRepository = forwardingRuleRepository;
    }

    private static readonly Dictionary<(CaseStatus From, CaseStatus To), HashSet<string>> Transitions = new()
    {
        // Coordinator / Female Coordinator verification.
        // The Proctor is included in every coordinator transition: a coordinator only *assists*
        // the Proctor, so the Proctor holds a superset of the coordinator's powers and can
        // verify / reject / hold / request resubmission on any case without it being forwarded.
        { (CaseStatus.Submitted, CaseStatus.Verified), new() { "coordinator", "female-coordinator", "proctor" } },
        { (CaseStatus.Submitted, CaseStatus.Rejected), new() { "coordinator", "female-coordinator", "proctor" } },
        { (CaseStatus.Submitted, CaseStatus.OnHold), new() { "coordinator", "female-coordinator", "proctor" } },
        { (CaseStatus.Submitted, CaseStatus.ResubmissionRequested), new() { "coordinator", "female-coordinator", "proctor" } },

        // Type-1 quick actions (Coordinator/Proctor/Deputy/Assistant can close, suggest as Type-2, or escalate to police)
        { (CaseStatus.Submitted, CaseStatus.SuggestedType2), new() { "coordinator", "female-coordinator", "proctor", "deputy-proctor", "assistant-proctor" } },
        { (CaseStatus.Submitted, CaseStatus.Closed), new() { "coordinator", "female-coordinator", "proctor", "deputy-proctor", "assistant-proctor" } },
        { (CaseStatus.Submitted, CaseStatus.PoliceCase), new() { "coordinator", "female-coordinator", "proctor", "deputy-proctor", "assistant-proctor" } },
        { (CaseStatus.Verified, CaseStatus.Closed), new() { "coordinator", "female-coordinator", "proctor", "deputy-proctor", "assistant-proctor" } },
        { (CaseStatus.SuggestedType2, CaseStatus.Closed), new() { "coordinator", "female-coordinator", "proctor", "deputy-proctor", "assistant-proctor" } },
        { (CaseStatus.ResubmissionRequested, CaseStatus.Submitted), new() { "student" } },
        { (CaseStatus.ResubmissionRequested, CaseStatus.Verified), new() { "coordinator", "female-coordinator", "proctor" } },
        { (CaseStatus.ResubmissionRequested, CaseStatus.Rejected), new() { "coordinator", "female-coordinator", "proctor" } },

        // Coordinator forwards verified case (sets to Assigned)
        { (CaseStatus.Verified, CaseStatus.Assigned), new() { "coordinator", "female-coordinator", "proctor", "sexual-harassment-committee" } },

        // Proctor actions
        { (CaseStatus.Assigned, CaseStatus.Resolved), new() { "proctor", "sexual-harassment-committee" } },
        { (CaseStatus.Assigned, CaseStatus.PoliceCase), new() { "proctor", "sexual-harassment-committee" } },
        { (CaseStatus.Assigned, CaseStatus.ForwardedToRegistrar), new() { "proctor", "sexual-harassment-committee" } },

        // Hearing workflow — the Proctor can run it directly, without waiting for a forward.
        { (CaseStatus.Assigned, CaseStatus.HearingScheduled), new() { "assistant-proctor", "proctor" } },
        { (CaseStatus.Verified, CaseStatus.HearingScheduled), new() { "assistant-proctor", "proctor" } },
        { (CaseStatus.HearingScheduled, CaseStatus.HearingCompleted), new() { "assistant-proctor", "proctor" } },

        // Deputy Proctor actions
        { (CaseStatus.HearingCompleted, CaseStatus.Assigned), new() { "deputy-proctor", "proctor" } },
        { (CaseStatus.HearingCompleted, CaseStatus.Resolved), new() { "deputy-proctor", "proctor" } },

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
        { (CaseStatus.OnHold, CaseStatus.Submitted), new() { "coordinator", "female-coordinator", "proctor" } },
        { (CaseStatus.OnHold, CaseStatus.Verified), new() { "coordinator", "female-coordinator", "proctor" } },
    };

    static WorkflowService()
    {
        // The Administrative Officer (legacy "coordinator" key) is the Proctor's assistant and
        // in practice runs the office, so they hold exactly the Proctor's transitions. Applied
        // here rather than by editing every entry above, so the two can never drift apart.
        foreach (var roles in Transitions.Values)
        {
            if (roles.Contains("proctor"))
                roles.Add("coordinator");
        }
    }

    public async Task<bool> ValidateTransitionAsync(CaseStatus from, CaseStatus to, string userRole, CaseType caseType)
    {
        if (userRole == "super-admin") return true;

        var permissionType = caseType == CaseType.Type1 ? "type-1" : "type-2";

        // These actions are permission-controlled. Check them before the legacy transition
        // map so a Type-2 grant can never make the same button appear on a Type-1 case.
        if (to == CaseStatus.Closed || to == CaseStatus.Resolved || to == CaseStatus.PoliceCase)
        {
            var closeRules = await _forwardingRuleRepository.FindAsync(r =>
                r.FromRole == userRole && r.ToRole == "__close__" && r.IsActive
                && r.AppliesToType == permissionType);
            return closeRules.Any();
        }

        if (to == CaseStatus.HearingScheduled)
        {
            var hearingRules = await _forwardingRuleRepository.FindAsync(r =>
                r.FromRole == userRole && r.ToRole == "__hearing__" && r.IsActive
                && r.AppliesToType == permissionType);
            return hearingRules.Any();
        }

        // Check hardcoded transitions first
        if (Transitions.TryGetValue((from, to), out var allowedRoles) && allowedRoles.Contains(userRole))
            return true;

        return false;
    }

    public async Task<CaseStatus?> GetForwardStatusAsync(string fromRole, string toRole, CaseStatus currentStatus, CaseType caseType)
    {
        var permissionType = caseType == CaseType.Type1 ? "type-1" : "type-2";
        // 1. Check DB-configured forwarding rules first.
        var rules = await _forwardingRuleRepository.FindAsync(
            r => r.FromRole == fromRole && r.ToRole == toRole && r.IsActive
                && r.AppliesToType == permissionType);
        var rule = rules.FirstOrDefault();

        if (rule is not null && !string.IsNullOrWhiteSpace(rule.ResultStatus))
        {
            try
            {
                return MappingExtensions.ParseEnum<CaseStatus>(rule.ResultStatus);
            }
            catch { /* fall through to hardcoded defaults */ }
        }

        // 2. Rule exists but no explicit status — default to Assigned.
        if (rule is not null)
            return CaseStatus.Assigned;

        // 3. No DB rule — hardcoded defaults (kept for backward compat).
        var hardcoded = (fromRole, toRole) switch
        {
            ("coordinator" or "female-coordinator", "proctor") => CaseStatus.Assigned,
            ("coordinator" or "female-coordinator", "sexual-harassment-committee") => CaseStatus.Assigned,
            ("proctor" or "coordinator", "assistant-proctor" or "deputy-proctor") => CaseStatus.Assigned,
            ("assistant-proctor", "deputy-proctor") => currentStatus,
            ("deputy-proctor", "assistant-proctor") => CaseStatus.Assigned,
            ("deputy-proctor", "proctor") => CaseStatus.Assigned,
            ("proctor" or "coordinator", "registrar") => CaseStatus.ForwardedToRegistrar,
            ("registrar", "proctor") => CaseStatus.Assigned,
            ("registrar", "disciplinary-committee") => CaseStatus.ForwardedToCommittee,
            ("proctor" or "sexual-harassment-committee", "disciplinary-committee") => CaseStatus.ForwardedToCommittee,
            ("sexual-harassment-committee", "assistant-proctor" or "deputy-proctor") => CaseStatus.Assigned,
            ("sexual-harassment-committee", "registrar") => CaseStatus.ForwardedToRegistrar,
            ("disciplinary-committee", "proctor") => CaseStatus.Assigned,
            _ => (CaseStatus?)null
        };
        if (hardcoded.HasValue) return hardcoded;

        // 4. Permissive fallback: any non-student source -> any non-student target
        //    is allowed with Assigned status. The forwardable-users dropdown lists
        //    every staff member, so refusing arbitrary forwards (e.g. coordinator
        //    -> assistant-proctor) leaves the dropdown showing users who can't
        //    actually receive a forward. Match the dropdown's contract here.
        //    Students are never a valid forward target (the controller's [Authorize]
        //    already excludes them as callers, but we double-check here).
        if (!string.IsNullOrWhiteSpace(toRole)
            && toRole != "student"
            && !string.IsNullOrWhiteSpace(fromRole))
        {
            return CaseStatus.Assigned;
        }

        return null;
    }
}
