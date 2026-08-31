using Microsoft.EntityFrameworkCore;
using PROCTOR.Domain.Entities;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Seeders;

public static class SystemSettingSeeder
{
    private static readonly Dictionary<string, (string Value, string Category, string Description)> RequiredSettings = new()
    {
        ["type1_forwarding_roles"] = ("proctor,coordinator,female-coordinator,deputy-proctor,assistant-proctor", "incident_routing", "Roles notified for Type-1 instant incidents"),
        ["case_viewing_type1"] = ("student,coordinator,proctor,assistant-proctor,deputy-proctor,registrar,disciplinary-committee,vc,super-admin", "case_viewing", "Roles that can see Type-1 cases"),
        ["case_viewing_type2"] = ("student,coordinator,proctor,assistant-proctor,deputy-proctor,registrar,disciplinary-committee,vc,super-admin", "case_viewing", "Roles that can see Type-2 cases"),
        ["case_viewing_confidential"] = ("proctor,coordinator,female-coordinator,sexual-harassment-committee,vc,super-admin", "case_viewing", "Roles that can see Confidential cases"),

        // Proctor Office 24/7 Control Room number. Included in the Type-1 acknowledgment
        // ("we are coming") so the complainant always has a live contact. Editable in Settings.
        ["control_room_number"] = ("+880 1847 140 016", "general", "24/7 Control Room contact number shown to complainants"),

        // AI report generation. The key is deliberately seeded EMPTY — it is a secret and is
        // pasted through Settings → AI Integration (or supplied via the GEMINI_API_KEY env var),
        // never committed to source control.
        ["ai_provider"] = ("gemini", "ai", "AI provider used for report generation"),
        ["ai_api_key"] = ("", "ai", "API key for the AI provider (write-only; never returned to clients)"),
        ["ai_model"] = ("gemini-2.5-flash-lite", "ai", "Model used for AI report generation"),
    };

    // Roles that must be on the Type-1 queue: an instant incident is an emergency and goes to
    // the whole proctorial team, both Administrative Officers included. Unlike Type-2 there is
    // no gender routing — that split applies only to formal complaints.
    private static readonly string[] RequiredType1Roles =
        ["proctor", "coordinator", "female-coordinator", "deputy-proctor", "assistant-proctor"];

    private const string Type1BackfillFlagKey = "type1_roles_backfilled";

    /// <summary>
    /// One-shot, additive top-up of <c>type1_forwarding_roles</c> on databases seeded before
    /// the full team was on the queue. Only adds missing roles — never removes any — and runs
    /// once, so an admin who later trims the list in Settings is not overridden.
    /// </summary>
    public static async Task BackfillType1RolesAsync(ProctorDbContext context)
    {
        var flag = await context.SystemSettings.FirstOrDefaultAsync(s => s.Key == Type1BackfillFlagKey);
        if (flag is { Value: "true" }) return;

        var setting = await context.SystemSettings.FirstOrDefaultAsync(s => s.Key == "type1_forwarding_roles");
        if (setting is not null)
        {
            var roles = setting.Value
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim())
                .Where(r => r.Length > 0)
                .ToList();

            var missing = RequiredType1Roles.Where(r => !roles.Contains(r, StringComparer.OrdinalIgnoreCase)).ToList();
            if (missing.Count > 0)
            {
                roles.AddRange(missing);
                setting.Value = string.Join(",", roles);
            }
        }

        if (flag is null)
        {
            context.SystemSettings.Add(new SystemSetting
            {
                Id = Guid.NewGuid(),
                Key = Type1BackfillFlagKey,
                Value = "true",
                Category = "internal",
                Description = "Guard flag: the Type-1 role queue has been topped up once."
            });
        }
        else
        {
            flag.Value = "true";
        }

        await context.SaveChangesAsync();
    }

    public static async Task SeedAsync(ProctorDbContext context)
    {
        var existingKeys = context.SystemSettings.Select(s => s.Key).ToHashSet();
        var newSettings = new List<SystemSetting>();

        foreach (var (key, (value, category, description)) in RequiredSettings)
        {
            if (existingKeys.Contains(key)) continue;

            newSettings.Add(new SystemSetting
            {
                Id = Guid.NewGuid(),
                Key = key,
                Value = value,
                Category = category,
                Description = description
            });
        }

        if (newSettings.Count > 0)
        {
            await context.SystemSettings.AddRangeAsync(newSettings);
            await context.SaveChangesAsync();
        }
    }
}
