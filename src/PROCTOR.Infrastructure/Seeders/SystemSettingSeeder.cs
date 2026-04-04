using PROCTOR.Domain.Entities;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Seeders;

public static class SystemSettingSeeder
{
    private static readonly Dictionary<string, (string Value, string Category, string Description)> RequiredSettings = new()
    {
        ["type1_forwarding_roles"] = ("proctor,deputy-proctor", "incident_routing", "Roles that receive Type-1 instant incidents"),
        ["case_viewing_type1"] = ("student,coordinator,proctor,assistant-proctor,deputy-proctor,registrar,disciplinary-committee,vc,super-admin", "case_viewing", "Roles that can see Type-1 cases"),
        ["case_viewing_type2"] = ("student,coordinator,proctor,assistant-proctor,deputy-proctor,registrar,disciplinary-committee,vc,super-admin", "case_viewing", "Roles that can see Type-2 cases"),
        ["case_viewing_confidential"] = ("proctor,female-coordinator,sexual-harassment-committee,vc,super-admin", "case_viewing", "Roles that can see Confidential cases"),
    };

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
