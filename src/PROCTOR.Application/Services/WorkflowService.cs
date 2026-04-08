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

    public async Task<bool> ValidateTransitionAsync(CaseStatus from, CaseStatus to, string userRole)
    {
        if (userRole == "super-admin") return true;

        // Check hardcoded transitions first
        if (Transitions.TryGetValue((from, to), out var allowedRoles) && allowedRoles.Contains(userRole))
            return true;

        // For close transitions, check dynamic __close__ rules
        if (to == CaseStatus.Closed)
        {
            var closeRules = await _forwardingRuleRepository.FindAsync(
                r => r.FromRole == userRole && r.ToRole == "__close__" && r.IsActive);
            if (closeRules.Any()) return true;
        }

        // For hearing transitions, check dynamic __hearing__ rules
        if (to == CaseStatus.HearingScheduled)
        {
            var hearingRules = await _forwardingRuleRepository.FindAsync(
                r => r.FromRole == userRole && r.ToRole == "__hearing__" && r.IsActive);
            if (hearingRules.Any()) return true;
        }

        return false;
    }

    public async Task<CaseStatus?> GetForwardStatusAsync(string fromRole, string toRole, CaseStatus currentStatus)
    {
        // Check DB-configured forwarding rules first
        var rules = await _forwardingRuleRepository.FindAsync(
            r => r.FromRole == fromRole && r.ToRole == toRole && r.IsActive);
        var rule = rules.FirstOrDefault();

        if (rule is not null && !string.IsNullOrWhiteSpace(rule.ResultStatus))
        {
            try
            {
                return MappingExtensions.ParseEnum<CaseStatus>(rule.ResultStatus);
            }
            catch { /* fall through to hardcoded defaults */ }
        }

        // If rule exists but has no explicit status, use "assigned" as default for forwarding
        if (rule is not null)
            return CaseStatus.Assigned;

        // Fallback: hardcoded defaults (kept for backward compat until all rules are migrated)
        return (fromRole, toRole) switch
        {
            ("coordinator" or "female-coordinator", "proctor") => CaseStatus.Assigned,
            ("coordinator" or "female-coordinator", "sexual-harassment-committee") => CaseStatus.Assigned,
            ("proctor", "assistant-proctor" or "deputy-proctor") => CaseStatus.Assigned,
            ("assistant-proctor", "deputy-proctor") => currentStatus,
            ("deputy-proctor", "assistant-proctor") => CaseStatus.Assigned,
            ("deputy-proctor", "proctor") => CaseStatus.Assigned,
            ("proctor", "registrar") => CaseStatus.ForwardedToRegistrar,
            ("registrar", "proctor") => CaseStatus.Assigned,
            ("registrar", "disciplinary-committee") => CaseStatus.ForwardedToCommittee,
            ("sexual-harassment-committee", "assistant-proctor" or "deputy-proctor") => CaseStatus.Assigned,
            ("sexual-harassment-committee", "registrar") => CaseStatus.ForwardedToRegistrar,
            ("disciplinary-committee", "proctor") => CaseStatus.Assigned,
            _ => null
        };
    }
}
