using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Seeders;

public static class UserSeeder
{
    public static async Task SeedAsync(ProctorDbContext context)
    {
        if (context.Users.Any()) return;

        var users = new (string Email, string Name, UserRole Role, string Password)[]
        {
            ("student@university.edu", "John Student", UserRole.Student, "Password123!"),
            ("coordinator@university.edu", "Sarah Coordinator", UserRole.Coordinator, "Password123!"),
            ("proctor@university.edu", "Dr. Michael Proctor", UserRole.Proctor, "Password123!"),
            ("assistant@university.edu", "Prof. Emily Assistant", UserRole.AssistantProctor, "Password123!"),
            ("deputy@university.edu", "Dr. Robert Deputy", UserRole.DeputyProctor, "Password123!"),
            ("registrar@university.edu", "Ms. Lisa Registrar", UserRole.Registrar, "Password123!"),
            ("dc@university.edu", "Committee Head", UserRole.DisciplinaryCommittee, "Password123!"),
            ("fcoord@university.edu", "Dr. Rachel Female Coord", UserRole.FemaleCoordinator, "Password123!"),
            ("shc@university.edu", "Committee SH", UserRole.SexualHarassmentCommittee, "Password123!"),
            ("vc@university.edu", "Vice Chancellor", UserRole.VC, "Password123!"),
            ("admin@university.edu", "System Administrator", UserRole.SuperAdmin, "Admin@123!")
        };

        foreach (var (email, name, role, password) in users)
        {
            context.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                Name = name,
                Role = role,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
    }
}
