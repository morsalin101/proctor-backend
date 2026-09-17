using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Seeders;

public static class RoleSeeder
{
    public static async Task SeedAsync(ProctorDbContext context)
    {
        if (context.Roles.Any()) return;

        var roles = Enum.GetValues<UserRole>().Select(role => new Role
        {
            Id = GetDeterministicGuid(role),
            RoleName = role,
            DisplayName = GetDisplayName(role),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await context.Roles.AddRangeAsync(roles);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Inserts Role rows for enum values added after the first seed (SeedAsync
    /// short-circuits once any role row exists).
    /// </summary>
    public static async Task BackfillMissingRolesAsync(ProctorDbContext context)
    {
        var existing = context.Roles.Select(r => r.RoleName).ToHashSet();
        var missing = Enum.GetValues<UserRole>()
            .Where(role => !existing.Contains(role))
            .Select(role => new Role
            {
                Id = GetDeterministicGuid(role),
                RoleName = role,
                DisplayName = GetDisplayName(role),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            })
            .ToList();

        if (missing.Count == 0) return;
        await context.Roles.AddRangeAsync(missing);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Idempotently refreshes DisplayName on existing databases (SeedAsync
    /// short-circuits once any role row exists).
    /// </summary>
    public static async Task BackfillDisplayNamesAsync(ProctorDbContext context)
    {
        var changed = false;
        foreach (var role in context.Roles.ToList())
        {
            var expected = GetDisplayName(role.RoleName);
            if (role.DisplayName == expected) continue;
            role.DisplayName = expected;
            role.UpdatedAt = DateTime.UtcNow;
            changed = true;
        }
        if (changed) await context.SaveChangesAsync();
    }

    private static string GetDisplayName(UserRole role) => role switch
    {
        // "Coordinator" is a legacy enum key: the role IS the Proctor Office's
        // Administrative Officer and carries the same power as the Proctor.
        UserRole.Coordinator => "Assistant Administrative Officer",
        UserRole.FemaleCoordinator => "Female Administrative Officer",
        _ => InsertSpaces(role.ToString())
    };

    public static Guid GetDeterministicGuid(UserRole role)
    {
        return new Guid($"00000000-0000-0000-0000-{((int)role + 1):D12}");
    }

    private static string InsertSpaces(string text)
    {
        return string.Concat(text.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
    }
}
