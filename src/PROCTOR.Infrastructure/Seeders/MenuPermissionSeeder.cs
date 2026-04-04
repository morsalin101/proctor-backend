using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Seeders;

public static class MenuPermissionSeeder
{
    private static readonly string[] AllMenuKeys =
    {
        "dashboard", "submit", "incidents", "cases", "hearings",
        "confidential", "monitoring", "reports", "users", "settings"
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
            ["settings"] = "RU"
        });

        AddPermissions(permissions, UserRole.Coordinator, new Dictionary<string, string>
        {
            ["dashboard"] = "R",
            ["submit"] = "CR",
            ["incidents"] = "R",
            ["cases"] = "RU",
            ["settings"] = "RU"
        });

        AddFullCrudPermissions(permissions, UserRole.Proctor);

        AddPermissions(permissions, UserRole.AssistantProctor, new Dictionary<string, string>
        {
            ["dashboard"] = "R",
            ["incidents"] = "R",
            ["cases"] = "RU",
            ["hearings"] = "CRUD",
            ["settings"] = "RU"
        });

        AddPermissions(permissions, UserRole.DeputyProctor, new Dictionary<string, string>
        {
            ["dashboard"] = "R",
            ["incidents"] = "R",
            ["cases"] = "CRUD",
            ["hearings"] = "CRUD",
            ["reports"] = "R",
            ["settings"] = "RU"
        });

        AddPermissions(permissions, UserRole.Registrar, new Dictionary<string, string>
        {
            ["dashboard"] = "R",
            ["cases"] = "RU",
            ["reports"] = "R",
            ["settings"] = "RU"
        });

        AddPermissions(permissions, UserRole.DisciplinaryCommittee, new Dictionary<string, string>
        {
            ["dashboard"] = "R",
            ["cases"] = "RU",
            ["hearings"] = "CRUD",
            ["reports"] = "R",
            ["settings"] = "RU"
        });

        AddPermissions(permissions, UserRole.FemaleCoordinator, new Dictionary<string, string>
        {
            ["dashboard"] = "R",
            ["cases"] = "RU",
            ["confidential"] = "CRUD",
            ["settings"] = "RU"
        });

        AddPermissions(permissions, UserRole.SexualHarassmentCommittee, new Dictionary<string, string>
        {
            ["dashboard"] = "R",
            ["cases"] = "RU",
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
