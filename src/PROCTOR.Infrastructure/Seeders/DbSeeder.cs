using Microsoft.Extensions.Logging;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Seeders;

public static class DbSeeder
{
    public static async Task SeedAsync(ProctorDbContext context, ILogger logger)
    {
        await RoleSeeder.SeedAsync(context);
        await UserSeeder.SeedAsync(context);
        await UserSeeder.BackfillGendersAsync(context);
        await MenuPermissionSeeder.SeedAsync(context);
        // Idempotently add missing my-cases / notifications rows for existing databases
        await MenuPermissionSeeder.BackfillMissingPermissionsAsync(context);
        await SystemSettingSeeder.SeedAsync(context);
        await ForwardingRuleSeeder.SeedAsync(context);
        await ForwardingRuleSeeder.SeedSpecialRulesAsync(context);
        await CaseCategorySeeder.SeedAsync(context);
        await StudentSeeder.SeedAsync(context);
        await CaseSubjectSeeder.SeedAsync(context);

        // One-shot data hygiene: delete notifications whose case has been removed.
        // Self-guarded by a SystemSetting flag — safe to run on every startup.
        await NotificationCleanup.CleanupOrphanedAsync(context, logger);
    }
}
