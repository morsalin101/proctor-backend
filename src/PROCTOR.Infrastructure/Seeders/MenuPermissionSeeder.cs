using Microsoft.EntityFrameworkCore;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Seeders;

public static class MenuPermissionSeeder
{
    private static readonly string[] AllMenuKeys =
    {
        "dashboard", "advanced-search", "submit", "incidents", "cases", "hearings",
        "confidential", "monitoring", "reports", "completed-reports", "users", "settings",
        "my-cases", "notifications", "registrar-reports", "vc-reports", "dc-reports",
        "dc-member-reports", "dcs-reports", "chairman-reports", "audit-logs"
    };
    private static readonly HashSet<string> Type3StageMenuKeys =
        ["registrar-reports", "vc-reports", "dc-reports", "dc-member-reports", "dcs-reports", "chairman-reports"];

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

        // The Administrative Officer ("coordinator") is the Proctor's assistant and in practice
        // runs the office, so the role mirrors the Proctor exactly — every menu, full CRUD,
        // the confidential queue included.
        AddFullCrudPermissions(permissions, UserRole.Coordinator);

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
            ["completed-reports"] = "R",
            ["settings"] = "RU"
        });

        AddPermissions(permissions, UserRole.Registrar, new Dictionary<string, string>
        {
            ["dashboard"] = "R",
            ["registrar-reports"] = "RU",
            ["notifications"] = "R",
            ["settings"] = "RU"
        });

        AddPermissions(permissions, UserRole.DisciplinaryCommittee, new Dictionary<string, string>
        {
            ["dashboard"] = "R",
            ["reports"] = "R",
            ["completed-reports"] = "R",
            ["settings"] = "RU"
        });

        // Female Coordinator mirrors Proctor's full access too (combined power), keeping the
        // "confidential" menu. Her case visibility stays restricted to the female track.
        AddFullCrudPermissions(permissions, UserRole.FemaleCoordinator);

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
            ["vc-reports"] = "RU",
            ["notifications"] = "R",
            ["settings"] = "RU"
        });

        AddPermissions(permissions, UserRole.DCChairman, new Dictionary<string, string>
        {
            ["dashboard"] = "R", ["dc-reports"] = "RU", ["notifications"] = "R", ["settings"] = "RU"
        });
        AddPermissions(permissions, UserRole.DCMember, new Dictionary<string, string>
        {
            ["dashboard"] = "R", ["dc-member-reports"] = "RU", ["notifications"] = "R", ["settings"] = "RU"
        });
        AddPermissions(permissions, UserRole.DCSecretary, new Dictionary<string, string>
        {
            ["dashboard"] = "R", ["dcs-reports"] = "CRU", ["notifications"] = "R", ["settings"] = "RU"
        });
        AddPermissions(permissions, UserRole.Chairman, new Dictionary<string, string>
        {
            ["dashboard"] = "R", ["chairman-reports"] = "RU", ["notifications"] = "R", ["settings"] = "RU"
        });

        AddFullCrudPermissions(permissions, UserRole.SuperAdmin);

        AddPermissions(permissions, UserRole.External, ExternalAccess);

        await context.MenuPermissions.AddRangeAsync(permissions);
        await context.SaveChangesAsync();
    }

    // An external participant called to a hearing sees only their own cases and the
    // hearings on them — no case list, no reports, no directory.
    private static readonly Dictionary<string, string> ExternalAccess = new()
    {
        ["dashboard"] = "R",
        ["my-cases"] = "R",
        ["notifications"] = "R",
        ["hearings"] = "R",
        ["settings"] = "RU"
    };

    /// <summary>
    /// Brings the Administrative Officer ("coordinator") up to full Proctor-equivalent access
    /// on databases seeded while the role was still restricted. Idempotent: existing rows are
    /// upgraded in place, missing ones inserted.
    /// </summary>
    public static async Task BackfillAdministrativeOfficerAsync(ProctorDbContext context)
    {
        var roleId = RoleSeeder.GetDeterministicGuid(UserRole.Coordinator);
        var existing = await context.MenuPermissions
            .Where(mp => mp.RoleId == roleId)
            .ToDictionaryAsync(mp => mp.MenuKey);

        var changed = false;
        foreach (var menuKey in AllMenuKeys)
        {
            if (Type3StageMenuKeys.Contains(menuKey) || menuKey == "audit-logs") continue;
            if (existing.TryGetValue(menuKey, out var row))
            {
                if (row.CanCreate && row.CanRead && row.CanUpdate && row.CanDelete) continue;
                row.CanCreate = row.CanRead = row.CanUpdate = row.CanDelete = true;
                row.UpdatedAt = DateTime.UtcNow;
                changed = true;
                continue;
            }

            context.MenuPermissions.Add(new MenuPermission
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
            changed = true;
        }

        if (changed) await context.SaveChangesAsync();
    }

    /// <summary>
    /// Give staff who can read cases the new search menu. Copy their case permissions so
    /// existing role customizations remain in effect on already-seeded databases.
    /// </summary>
    public static async Task BackfillAdvancedSearchAsync(ProctorDbContext context)
    {
        var staffRoles = new[]
        {
            UserRole.Coordinator, UserRole.Proctor, UserRole.AssistantProctor,
            UserRole.DeputyProctor, UserRole.Registrar, UserRole.DisciplinaryCommittee,
            UserRole.FemaleCoordinator, UserRole.SexualHarassmentCommittee,
            UserRole.VC, UserRole.SuperAdmin
        };
        var roleIds = staffRoles.Select(RoleSeeder.GetDeterministicGuid).ToArray();
        var casePermissions = await context.MenuPermissions
            .Where(p => roleIds.Contains(p.RoleId) && p.MenuKey == "cases" && p.CanRead)
            .ToListAsync();
        var existing = await context.MenuPermissions
            .Where(p => roleIds.Contains(p.RoleId) && p.MenuKey == "advanced-search")
            .Select(p => p.RoleId).ToListAsync();
        var existingIds = existing.ToHashSet();
        var additions = casePermissions.Where(p => !existingIds.Contains(p.RoleId))
            .Select(p => new MenuPermission
            {
                Id = Guid.NewGuid(), RoleId = p.RoleId, MenuKey = "advanced-search",
                CanCreate = p.CanCreate, CanRead = p.CanRead,
                CanUpdate = p.CanUpdate, CanDelete = p.CanDelete,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            }).ToList();
        if (additions.Count == 0) return;
        await context.MenuPermissions.AddRangeAsync(additions);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Existing roles keep the same access to finalized reports that they already had to the
    /// original reports page. The two permissions can be changed independently afterwards.
    /// </summary>
    public static async Task BackfillCompletedReportsAsync(ProctorDbContext context)
    {
        var reportPermissions = await context.MenuPermissions
            .Where(p => p.MenuKey == "reports")
            .ToListAsync();
        var existingRoleIds = (await context.MenuPermissions
            .Where(p => p.MenuKey == "completed-reports")
            .Select(p => p.RoleId)
            .ToListAsync())
            .ToHashSet();

        var additions = reportPermissions
            .Where(p => !existingRoleIds.Contains(p.RoleId))
            .Select(p => new MenuPermission
            {
                Id = Guid.NewGuid(),
                RoleId = p.RoleId,
                MenuKey = "completed-reports",
                CanCreate = p.CanCreate,
                CanRead = p.CanRead,
                CanUpdate = p.CanUpdate,
                CanDelete = p.CanDelete,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            })
            .ToList();

        if (additions.Count == 0) return;
        await context.MenuPermissions.AddRangeAsync(additions);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Grants the External role its menus on databases seeded before the role existed.
    /// Idempotent: skips any (role, menuKey) pair already present.
    /// </summary>
    public static async Task BackfillExternalRoleAsync(ProctorDbContext context)
    {
        var roleId = RoleSeeder.GetDeterministicGuid(UserRole.External);
        var newRows = new List<MenuPermission>();

        foreach (var (menuKey, access) in ExternalAccess)
        {
            var exists = await context.MenuPermissions
                .AnyAsync(mp => mp.RoleId == roleId && mp.MenuKey == menuKey);
            if (exists) continue;

            newRows.Add(new MenuPermission
            {
                Id = Guid.NewGuid(),
                RoleId = roleId,
                MenuKey = menuKey,
                CanCreate = access.Contains('C'),
                CanRead = access.Contains('R'),
                CanUpdate = access.Contains('U'),
                CanDelete = access.Contains('D'),
                CanSend = access.Contains('S'),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        if (newRows.Count == 0) return;
        await context.MenuPermissions.AddRangeAsync(newRows);
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
                CanSend = access.Contains('S'),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }

    // Inserts missing rows for existing roles on already-seeded databases (SeedAsync bails out
    // as soon as any permission row exists, so new menus never reach an existing install).
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

        // Both coordinators write investigation reports, so the Reports menu is theirs by
        // default with full CRUD — same as the Proctor they assist.
        foreach (var role in new[] { UserRole.Coordinator, UserRole.FemaleCoordinator })
        {
            var roleId = RoleSeeder.GetDeterministicGuid(role);
            var exists = await context.MenuPermissions
                .AnyAsync(mp => mp.RoleId == roleId && mp.MenuKey == "reports");
            if (exists) continue;

            newRows.Add(new MenuPermission
            {
                Id = Guid.NewGuid(),
                RoleId = roleId,
                MenuKey = "reports",
                CanCreate = true,
                CanRead = true,
                CanUpdate = true,
                CanDelete = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        if (newRows.Count > 0)
        {
            await context.MenuPermissions.AddRangeAsync(newRows);
            await context.SaveChangesAsync();
        }
    }

    private static void AddFullCrudPermissions(List<MenuPermission> permissions, UserRole role, params string[] excludeMenuKeys)
    {
        var roleId = RoleSeeder.GetDeterministicGuid(role);

        foreach (var menuKey in AllMenuKeys)
        {
            if (excludeMenuKeys.Contains(menuKey)) continue;
            if (role != UserRole.SuperAdmin && Type3StageMenuKeys.Contains(menuKey)) continue;
            if (role != UserRole.SuperAdmin && menuKey == "audit-logs") continue;

            var auditReadOnly = menuKey == "audit-logs";

            permissions.Add(new MenuPermission
            {
                Id = Guid.NewGuid(),
                RoleId = roleId,
                MenuKey = menuKey,
                CanCreate = !auditReadOnly,
                CanRead = true,
                CanUpdate = !auditReadOnly,
                CanDelete = !auditReadOnly,
                CanSend = menuKey == "completed-reports" && role is UserRole.Proctor or UserRole.SuperAdmin,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }

    public static async Task BackfillAuditLogPermissionAsync(ProctorDbContext context)
    {
        var roleId = RoleSeeder.GetDeterministicGuid(UserRole.SuperAdmin);
        if (await context.MenuPermissions.AnyAsync(x => x.RoleId == roleId && x.MenuKey == "audit-logs")) return;

        context.MenuPermissions.Add(new MenuPermission
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            MenuKey = "audit-logs",
            CanRead = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    public static async Task BackfillType3PermissionsAsync(ProctorDbContext context)
    {
        const string flagKey = "type3_permissions_backfilled";
        if (await context.SystemSettings.AnyAsync(x => x.Key == flagKey && x.Value == "true")) return;
        var grants = new Dictionary<UserRole, (string Menu, string Access)[]>
        {
            [UserRole.Registrar] = [("registrar-reports", "RU")],
            [UserRole.VC] = [("vc-reports", "RU")],
            [UserRole.DCChairman] = [("dashboard", "R"), ("dc-reports", "RU"), ("notifications", "R"), ("settings", "RU")],
            [UserRole.DCMember] = [("dashboard", "R"), ("dc-member-reports", "RU"), ("notifications", "R"), ("settings", "RU")],
            [UserRole.DCSecretary] = [("dashboard", "R"), ("dcs-reports", "CRU"), ("notifications", "R"), ("settings", "RU")],
            [UserRole.Chairman] = [("dashboard", "R"), ("chairman-reports", "RU"), ("notifications", "R"), ("settings", "RU")]
        };

        foreach (var (role, rows) in grants)
        {
            var roleId = RoleSeeder.GetDeterministicGuid(role);
            foreach (var (menu, access) in rows)
            {
                if (await context.MenuPermissions.AnyAsync(x => x.RoleId == roleId && x.MenuKey == menu)) continue;
                context.MenuPermissions.Add(new MenuPermission
                {
                    Id = Guid.NewGuid(), RoleId = roleId, MenuKey = menu,
                    CanCreate = access.Contains('C'), CanRead = access.Contains('R'),
                    CanUpdate = access.Contains('U'), CanDelete = access.Contains('D'), CanSend = access.Contains('S')
                });
            }
        }

        foreach (var role in new[] { UserRole.Proctor, UserRole.SuperAdmin })
        {
            var roleId = RoleSeeder.GetDeterministicGuid(role);
            var permission = await context.MenuPermissions.FirstOrDefaultAsync(x => x.RoleId == roleId && x.MenuKey == "completed-reports");
            if (permission is null)
                context.MenuPermissions.Add(new MenuPermission { Id = Guid.NewGuid(), RoleId = roleId, MenuKey = "completed-reports", CanRead = true, CanSend = true });
            else
                permission.CanSend = true;
        }

        context.SystemSettings.Add(new SystemSetting
        {
            Id = Guid.NewGuid(), Key = flagKey, Value = "true", Category = "internal",
            Description = "Guard flag: Type-3 menus and initial Send permissions were seeded once."
        });

        await context.SaveChangesAsync();
    }
}
