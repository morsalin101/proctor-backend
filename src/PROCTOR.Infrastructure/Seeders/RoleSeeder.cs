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
            DisplayName = InsertSpaces(role.ToString()),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await context.Roles.AddRangeAsync(roles);
        await context.SaveChangesAsync();
    }

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
