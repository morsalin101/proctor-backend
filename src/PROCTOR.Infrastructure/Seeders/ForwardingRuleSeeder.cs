using Microsoft.EntityFrameworkCore;
using PROCTOR.Domain.Entities;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Seeders;

public static class ForwardingRuleSeeder
{
    public static async Task SeedAsync(ProctorDbContext context)
    {
        if (await context.ForwardingRules.AnyAsync()) return;

        var rules = new List<ForwardingRule>
        {
            // Coordinator can hand off to any staff role (matches the forwardable-users
            // dropdown which lists every non-student). Each rule has an explicit
            // ResultStatus so it shows up in Settings → Forwarding Rules.
            new() { Id = Guid.NewGuid(), FromRole = "coordinator", ToRole = "proctor", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "coordinator", ToRole = "assistant-proctor", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "coordinator", ToRole = "deputy-proctor", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "coordinator", ToRole = "registrar", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "coordinator", ToRole = "disciplinary-committee", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "coordinator", ToRole = "sexual-harassment-committee", ResultStatus = "assigned" },
            // Female-coordinator mirrors coordinator (with the gender-routed confidential path)
            new() { Id = Guid.NewGuid(), FromRole = "female-coordinator", ToRole = "proctor", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "female-coordinator", ToRole = "assistant-proctor", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "female-coordinator", ToRole = "deputy-proctor", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "female-coordinator", ToRole = "registrar", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "female-coordinator", ToRole = "disciplinary-committee", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "female-coordinator", ToRole = "sexual-harassment-committee", ResultStatus = "assigned" },
            // Proctor + Deputy + Assistant <-> each other + committees + registrar
            new() { Id = Guid.NewGuid(), FromRole = "proctor", ToRole = "assistant-proctor", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "proctor", ToRole = "deputy-proctor", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "proctor", ToRole = "registrar", ResultStatus = "forwarded-to-registrar" },
            new() { Id = Guid.NewGuid(), FromRole = "proctor", ToRole = "disciplinary-committee", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "assistant-proctor", ToRole = "deputy-proctor", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "assistant-proctor", ToRole = "proctor", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "assistant-proctor", ToRole = "registrar", ResultStatus = "forwarded-to-registrar" },
            new() { Id = Guid.NewGuid(), FromRole = "deputy-proctor", ToRole = "assistant-proctor", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "deputy-proctor", ToRole = "proctor", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "deputy-proctor", ToRole = "registrar", ResultStatus = "forwarded-to-registrar" },
            new() { Id = Guid.NewGuid(), FromRole = "deputy-proctor", ToRole = "disciplinary-committee", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "registrar", ToRole = "proctor", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "registrar", ToRole = "disciplinary-committee", ResultStatus = "forwarded-to-committee" },
            new() { Id = Guid.NewGuid(), FromRole = "sexual-harassment-committee", ToRole = "assistant-proctor", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "sexual-harassment-committee", ToRole = "deputy-proctor", ResultStatus = "assigned" },
            new() { Id = Guid.NewGuid(), FromRole = "sexual-harassment-committee", ToRole = "registrar", ResultStatus = "forwarded-to-registrar" },
            new() { Id = Guid.NewGuid(), FromRole = "disciplinary-committee", ToRole = "proctor", ResultStatus = "assigned" },
        };

        await context.ForwardingRules.AddRangeAsync(rules);
        await context.SaveChangesAsync();
    }

    // Naming who handles a case is an office decision: only the Proctor and the
    // Administrative Officers assign. The Assistant / Deputy Proctors are the people being
    // assigned, so they do not hold the permission themselves.
    private static readonly string[] AssignerRoles =
        ["proctor", "coordinator", "female-coordinator", "super-admin"];

    private const string AssignRolesFixFlagKey = "assign_roles_restricted";

    /// <summary>
    /// One-shot correction for databases seeded while the Assistant / Deputy Proctors also
    /// held the assign permission. Guarded by a flag so an admin who re-grants it in
    /// Settings → Forwarding Rules is not overridden on the next restart.
    /// </summary>
    public static async Task RestrictAssignRolesAsync(ProctorDbContext context)
    {
        var flag = await context.SystemSettings.FirstOrDefaultAsync(s => s.Key == AssignRolesFixFlagKey);
        if (flag is { Value: "true" }) return;

        var stale = await context.ForwardingRules
            .Where(r => r.ToRole == "__assign__" && !AssignerRoles.Contains(r.FromRole))
            .ToListAsync();
        if (stale.Count > 0)
            context.ForwardingRules.RemoveRange(stale);

        foreach (var fromRole in AssignerRoles)
        {
            var exists = await context.ForwardingRules
                .AnyAsync(r => r.FromRole == fromRole && r.ToRole == "__assign__");
            if (exists) continue;

            await context.ForwardingRules.AddAsync(new ForwardingRule
            {
                Id = Guid.NewGuid(),
                FromRole = fromRole,
                ToRole = "__assign__",
                ResultStatus = "assigned"
            });
        }

        if (flag is null)
        {
            context.SystemSettings.Add(new SystemSetting
            {
                Id = Guid.NewGuid(),
                Key = AssignRolesFixFlagKey,
                Value = "true",
                Category = "internal",
                Description = "Guard flag: the assign permission has been restricted to the office roles once."
            });
        }
        else
        {
            flag.Value = "true";
        }

        await context.SaveChangesAsync();
    }

    // Seed __close__ and __hearing__ special rules (runs even if forwarding rules exist)
    public static async Task SeedSpecialRulesAsync(ProctorDbContext context)
    {
        var hasClose = await context.ForwardingRules.AnyAsync(r => r.ToRole == "__close__");
        if (!hasClose)
        {
            var closeRules = new List<ForwardingRule>
            {
                new() { Id = Guid.NewGuid(), FromRole = "proctor", ToRole = "__close__", ResultStatus = "closed" },
                new() { Id = Guid.NewGuid(), FromRole = "coordinator", ToRole = "__close__", ResultStatus = "closed" },
                new() { Id = Guid.NewGuid(), FromRole = "sexual-harassment-committee", ToRole = "__close__", ResultStatus = "closed" },
                new() { Id = Guid.NewGuid(), FromRole = "disciplinary-committee", ToRole = "__close__", ResultStatus = "closed" },
                new() { Id = Guid.NewGuid(), FromRole = "super-admin", ToRole = "__close__", ResultStatus = "closed" },
            };
            await context.ForwardingRules.AddRangeAsync(closeRules);
        }

        var hasHearing = await context.ForwardingRules.AnyAsync(r => r.ToRole == "__hearing__");
        if (!hasHearing)
        {
            var hearingRules = new List<ForwardingRule>
            {
                new() { Id = Guid.NewGuid(), FromRole = "assistant-proctor", ToRole = "__hearing__", ResultStatus = "hearing-scheduled" },
                new() { Id = Guid.NewGuid(), FromRole = "proctor", ToRole = "__hearing__", ResultStatus = "hearing-scheduled" },
                new() { Id = Guid.NewGuid(), FromRole = "coordinator", ToRole = "__hearing__", ResultStatus = "hearing-scheduled" },
                new() { Id = Guid.NewGuid(), FromRole = "deputy-proctor", ToRole = "__hearing__", ResultStatus = "hearing-scheduled" },
                new() { Id = Guid.NewGuid(), FromRole = "disciplinary-committee", ToRole = "__hearing__", ResultStatus = "hearing-scheduled" },
            };
            await context.ForwardingRules.AddRangeAsync(hearingRules);
        }

        var hasAssign = await context.ForwardingRules.AnyAsync(r => r.ToRole == "__assign__");
        if (!hasAssign)
        {
            var assignRules = AssignerRoles.Select(fromRole => new ForwardingRule
            {
                Id = Guid.NewGuid(),
                FromRole = fromRole,
                ToRole = "__assign__",
                ResultStatus = "assigned"
            }).ToList();
            await context.ForwardingRules.AddRangeAsync(assignRules);
        }

        // The Administrative Officer ("coordinator") carries the Proctor's powers, so they get
        // the Proctor's special rules too. Checked per (role, rule) rather than "does any row
        // with this ToRole exist", so existing databases pick these up as well.
        var administrativeOfficerRules = new (string ToRole, string ResultStatus)[]
        {
            ("__close__", "closed"),
            ("__hearing__", "hearing-scheduled"),
        };
        foreach (var (toRole, resultStatus) in administrativeOfficerRules)
        {
            var exists = await context.ForwardingRules
                .AnyAsync(r => r.FromRole == "coordinator" && r.ToRole == toRole);
            if (exists) continue;

            await context.ForwardingRules.AddAsync(new ForwardingRule
            {
                Id = Guid.NewGuid(),
                FromRole = "coordinator",
                ToRole = toRole,
                ResultStatus = resultStatus
            });
        }

        // Checked per role rather than "does any __draft_report__ row exist", so roles added
        // later (the coordinators) also reach databases that were seeded before them.
        var draftReportRoles = new[]
        {
            "proctor", "deputy-proctor", "assistant-proctor", "disciplinary-committee",
            "coordinator", "female-coordinator", "super-admin"
        };
        foreach (var fromRole in draftReportRoles)
        {
            var exists = await context.ForwardingRules
                .AnyAsync(r => r.FromRole == fromRole && r.ToRole == "__draft_report__");
            if (exists) continue;

            await context.ForwardingRules.AddAsync(new ForwardingRule
            {
                Id = Guid.NewGuid(),
                FromRole = fromRole,
                ToRole = "__draft_report__",
                ResultStatus = "draft-report"
            });
        }

        await context.SaveChangesAsync();
    }
}
