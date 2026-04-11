using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Seeders;

public static class DbSeeder
{
    public static async Task SeedAsync(ProctorDbContext context)
    {
        await RoleSeeder.SeedAsync(context);
        await UserSeeder.SeedAsync(context);
        await MenuPermissionSeeder.SeedAsync(context);
        // Idempotently add missing my-cases / notifications rows for existing databases
        await MenuPermissionSeeder.BackfillMissingPermissionsAsync(context);
        await SystemSettingSeeder.SeedAsync(context);
        await ForwardingRuleSeeder.SeedAsync(context);
        await ForwardingRuleSeeder.SeedSpecialRulesAsync(context);
    }
}
