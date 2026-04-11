using Microsoft.EntityFrameworkCore;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Seeders;

public static class MenuPermissionSeeder
{
    private static readonly string[] AllMenuKeys =
    {
        "dashboard", "submit", "incidents", "cases", "hearings",
        "confidential", "monitoring", "reports", "users", "settings",
        "my-cases", "notifications"
    };

    public static async Task SeedAsync(ProctorDbContext context)
    {
        if (context.MenuPermissions.Any()) return;

        var permissions = new List<MenuPermission>();

        AddPermissions(permissions, UserRole.Student, new Dictionary<string, string>
        {
            ["dashboard"] = "R",
            ["submit"] = "CR",
            ["cases"] = "R",
            ["my-cases"] = "R",
            ["notifications"] = "R",
            ["settings"] = "RU"
        });

        AddPermissions(permissions, UserRole.Coordinator, new Dictionary<string, string>
        {
            ["dashboard"] = "R",
            ["submit"] = "CR",
            ["incidents"] = "R",
            ["cases"] = "RU",
            ["my-cases"] = "R",
            ["notifications"] = "R",
            ["settings"] = "RU"
        });

        AddFullCrudPermissions(permissions, UserRole.Proctor);

        AddPermissions(permissions, UserRole.AssistantProctor, new Dictionary<string, string>
        {
            ["dashboard"] = "R",
            ["incidents"] = "R",
            ["cases"] = "RU",
            ["my-cases"] = "R",
            ["notifications"] = "R",
            ["hearings"] = "CRUD",
            ["settings"] = "RU"
        });

        AddPermissions(permissions, UserRole.DeputyProctor, new Dictionary<string, string>
        {
            ["dashboard"] = "R",
            ["incidents"] = "R",
            ["cases"] = "CRUD",
            ["my-cases"] = "R",
            ["notifications"] = "R",
            ["hearings"] = "CRUD",
            ["reports"] = "R",
            ["settings"] = "RU"
        });

        AddPermissions(permissions, UserRole.Registrar, new Dictionary<string, string>
        {
            ["dashboard"] = "R",
            ["cases"] = "RU",
            ["my-cases"] = "R",
            ["notifications"] = "R",
            ["reports"] = "R",
            ["settings"] = "RU"
        });

        AddPermissions(permissions, UserRole.DisciplinaryCommittee, new Dictionary<string, string>
        {
            ["dashboard"] = "R",
            ["cases"] = "RU",
            ["my-cases"] = "R",
            ["notifications"] = "R",
            ["hearings"] = "CRUD",
            ["reports"] = "R",
            ["settings"] = "RU"
        });

        AddPermissions(permissions, UserRole.FemaleCoordinator, new Dictionary<string, string>
        {
            ["dashboard"] = "R",
            ["cases"] = "RU",
            ["my-cases"] = "R",
            ["notifications"] = "R",
            ["confidential"] = "CRUD",
            ["settings"] = "RU"
        });

        AddPermissions(permissions, UserRole.SexualHarassmentCommittee, new Dictionary<string, string>
        {
            ["dashboard"] = "R",
            ["cases"] = "RU",
            ["my-cases"] = "R",
            ["notifications"] = "R",
            ["confidential"] = "CRUD",
            ["settings"] = "RU"
        });

        AddPermissions(permissions, UserRole.VC, new Dictionary<string, string>
        {
            ["dashboard"] = "R",
            ["incidents"] = "R",
            ["cases"] = "R",
            ["confidential"] = "R",
            ["monitoring"] = "CRUD",
            ["reports"] = "R",
            ["users"] = "R",
            ["settings"] = "RU"
        });

        AddFullCrudPermissions(permissions, UserRole.SuperAdmin);

        await context.MenuPermissions.AddRangeAsync(permissions);
        await context.SaveChangesAsync();
    }

    private static void AddPermissions(
        List<MenuPermission> permissions,
        UserRole role,
        Dictionary<string, string> menuPermissions)
    {
        var roleId = RoleSeeder.GetDeterministicGuid(role);

        foreach (var (menuKey, access) in menuPermissions)
        {
            permissions.Add(new MenuPermission
            {
                Id = Guid.NewGuid(),
                RoleId = roleId,
                MenuKey = menuKey,
                CanCreate = access.Contains('C'),
                CanRead = access.Contains('R'),
                CanUpdate = access.Contains('U'),
                CanDelete = access.Contains('D'),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }

    // Inserts missing my-cases / notifications rows for existing roles on already-seeded databases.
    // Idempotent: skips any (role, menuKey) pair that already exists. Called from Program.cs after SeedAsync.
    public static async Task BackfillMissingPermissionsAsync(ProctorDbContext context)
    {
        var defaultMenuKeys = new[] { "my-cases", "notifications" };
        var rolesToBackfill = new[]
        {
            UserRole.Student, UserRole.Coordinator, UserRole.AssistantProctor, UserRole.DeputyProctor,
            UserRole.Registrar, UserRole.DisciplinaryCommittee, UserRole.FemaleCoordinator,
            UserRole.SexualHarassmentCommittee
        };

        var newRows = new List<MenuPermission>();
        foreach (var role in rolesToBackfill)
        {
            var roleId = RoleSeeder.GetDeterministicGuid(role);
            foreach (var menuKey in defaultMenuKeys)
            {
                var exists = await context.MenuPermissions
                    .AnyAsync(mp => mp.RoleId == roleId && mp.MenuKey == menuKey);
                if (exists) continue;

                newRows.Add(new MenuPermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = roleId,
                    MenuKey = menuKey,
                    CanCreate = false,
                    CanRead = true,
                    CanUpdate = false,
                    CanDelete = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        if (newRows.Count > 0)
        {
            await context.MenuPermissions.AddRangeAsync(newRows);
            await context.SaveChangesAsync();
        }
    }

    private static void AddFullCrudPermissions(List<MenuPermission> permissions, UserRole role)
    {
        var roleId = RoleSeeder.GetDeterministicGuid(role);

        foreach (var menuKey in AllMenuKeys)
        {
            permissions.Add(new MenuPermission
            {
                Id = Guid.NewGuid(),
                RoleId = roleId,
                MenuKey = menuKey,
                CanCreate = true,
                CanRead = true,
                CanUpdate = true,
                CanDelete = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }
}
