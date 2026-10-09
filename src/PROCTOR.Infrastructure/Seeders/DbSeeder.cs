using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using PROCTOR.Infrastructure.Data;
using PROCTOR.Domain.Enums;

namespace PROCTOR.Infrastructure.Seeders;

public static class DbSeeder
{
    public static async Task SeedAsync(ProctorDbContext context, ILogger logger)
    {
        await RoleSeeder.SeedAsync(context);
        await RoleSeeder.BackfillMissingRolesAsync(context);
        await RoleSeeder.BackfillDisplayNamesAsync(context);
        await UserSeeder.SeedAsync(context);
        await UserSeeder.BackfillGendersAsync(context);
        await ProctorialBodySeeder.SeedAsync(context);
        await MenuPermissionSeeder.SeedAsync(context);
        // Idempotently add missing my-cases / notifications rows for existing databases
        await MenuPermissionSeeder.BackfillMissingPermissionsAsync(context);
        await MenuPermissionSeeder.BackfillExternalRoleAsync(context);
        await MenuPermissionSeeder.BackfillAdministrativeOfficerAsync(context);
        await MenuPermissionSeeder.BackfillAdvancedSearchAsync(context);
        await MenuPermissionSeeder.BackfillCompletedReportsAsync(context);
        await MenuPermissionSeeder.BackfillType3PermissionsAsync(context);
        await MenuPermissionSeeder.BackfillAuditLogPermissionAsync(context);
        // A Type-3 workflow moves the finalized report through the approval chain. The linked
        // case keeps its other details, but its case type must always identify it as Type-3.
        await context.Cases
            .Where(c => c.Type3Workflow != null && c.Type != CaseType.Type3)
            .ExecuteUpdateAsync(update => update.SetProperty(c => c.Type, CaseType.Type3));
        await SystemSettingSeeder.SeedAsync(context);
        await SystemSettingSeeder.BackfillType1RolesAsync(context);
        await ForwardingRuleSeeder.SeedAsync(context);
        await ForwardingRuleSeeder.SeedSpecialRulesAsync(context);
        await ForwardingRuleSeeder.RestrictAssignRolesAsync(context);
        await CaseCategorySeeder.SeedAsync(context);
        await StudentSeeder.SeedAsync(context);
        await StudentSeeder.BackfillCaseCgpaAsync(context);
        await CaseCategorySeeder.SeedBanglaAndMapAsync(context);
        await CaseSeeder.SeedAsync(context);

        // One-shot data hygiene: delete notifications whose case has been removed.
        // Self-guarded by a SystemSetting flag — safe to run on every startup.
        await NotificationCleanup.CleanupOrphanedAsync(context, logger);
    }
}
