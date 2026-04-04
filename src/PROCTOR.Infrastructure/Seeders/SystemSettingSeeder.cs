using PROCTOR.Domain.Entities;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Seeders;

public static class SystemSettingSeeder
{
    public static async Task SeedAsync(ProctorDbContext context)
    {
        if (context.SystemSettings.Any()) return;

        var settings = new List<SystemSetting>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Key = "type1_forwarding_roles",
                Value = "proctor,deputy-proctor",
                Category = "incident_routing",
                Description = "Roles that receive Type-1 instant incidents"
            }
        };

        await context.SystemSettings.AddRangeAsync(settings);
        await context.SaveChangesAsync();
    }
}
