using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Seeders;

public static class UserSeeder
{
    public static async Task SeedAsync(ProctorDbContext context)
    {
        var users = new (string Email, string Name, UserRole Role, Gender Gender, string Password)[]
        {
            // Students (mixed gender) — 8 total
            ("student@university.edu", "John Student", UserRole.Student, Gender.Male, "Password123!"),
            ("student2@university.edu", "Aisha Rahman", UserRole.Student, Gender.Female, "Password123!"),
            ("student3@university.edu", "Tariq Hossain", UserRole.Student, Gender.Male, "Password123!"),
            ("student4@university.edu", "Nadia Akter", UserRole.Student, Gender.Female, "Password123!"),
            ("student5@university.edu", "Rafiq Ahmed", UserRole.Student, Gender.Male, "Password123!"),
            ("student6@university.edu", "Mehedi Hassan", UserRole.Student, Gender.Male, "Password123!"),
            ("student7@university.edu", "Fatima Karim", UserRole.Student, Gender.Female, "Password123!"),
            ("student8@university.edu", "Sumaiya Islam", UserRole.Student, Gender.Female, "Password123!"),

            // Coordinators — 3 total
            ("coordinator@university.edu", "Sarah Coordinator", UserRole.Coordinator, Gender.Female, "Password123!"),
            ("coordinator2@university.edu", "Imran Khan", UserRole.Coordinator, Gender.Male, "Password123!"),
            ("coordinator3@university.edu", "Tanveer Chowdhury", UserRole.Coordinator, Gender.Male, "Password123!"),

            // Female Coordinators — 2 total
            ("fcoord@university.edu", "Dr. Rachel Female Coord", UserRole.FemaleCoordinator, Gender.Female, "Password123!"),
            ("fcoord2@university.edu", "Dr. Shabnam Akter", UserRole.FemaleCoordinator, Gender.Female, "Password123!"),

            // Proctors — 3 total
            ("proctor@university.edu", "Dr. Michael Proctor", UserRole.Proctor, Gender.Male, "Password123!"),
            ("proctor2@university.edu", "Dr. Anika Tahsin", UserRole.Proctor, Gender.Female, "Password123!"),
            ("proctor3@university.edu", "Dr. Habib Rahman", UserRole.Proctor, Gender.Male, "Password123!"),

            // Assistant Proctors — 5 total
            ("assistant@university.edu", "Prof. Emily Assistant", UserRole.AssistantProctor, Gender.Female, "Password123!"),
            ("assistant2@university.edu", "Mr. Karim Sheikh", UserRole.AssistantProctor, Gender.Male, "Password123!"),
            ("assistant3@university.edu", "Ms. Farzana Begum", UserRole.AssistantProctor, Gender.Female, "Password123!"),
            ("assistant4@university.edu", "Mr. Rakib Mahmud", UserRole.AssistantProctor, Gender.Male, "Password123!"),
            ("assistant5@university.edu", "Ms. Nusrat Jahan", UserRole.AssistantProctor, Gender.Female, "Password123!"),

            // Deputy Proctors — 5 total
            ("deputy@university.edu", "Dr. Robert Deputy", UserRole.DeputyProctor, Gender.Male, "Password123!"),
            ("deputy2@university.edu", "Mr. Aminul Islam", UserRole.DeputyProctor, Gender.Male, "Password123!"),
            ("deputy3@university.edu", "Ms. Sabrina Hossain", UserRole.DeputyProctor, Gender.Female, "Password123!"),
            ("deputy4@university.edu", "Mr. Shahidul Alam", UserRole.DeputyProctor, Gender.Male, "Password123!"),
            ("deputy5@university.edu", "Ms. Rumana Sultana", UserRole.DeputyProctor, Gender.Female, "Password123!"),

            // Registrars — 2 total
            ("registrar@university.edu", "Ms. Lisa Registrar", UserRole.Registrar, Gender.Female, "Password123!"),
            ("registrar2@university.edu", "Mr. Mahbub Alam", UserRole.Registrar, Gender.Male, "Password123!"),

            // Disciplinary Committee — 3 total
            ("dc@university.edu", "Committee Head", UserRole.DisciplinaryCommittee, Gender.Male, "Password123!"),
            ("dc2@university.edu", "Prof. Selim Ahmed", UserRole.DisciplinaryCommittee, Gender.Male, "Password123!"),
            ("dc3@university.edu", "Prof. Roksana Begum", UserRole.DisciplinaryCommittee, Gender.Female, "Password123!"),

            // Sexual Harassment Committee — 3 total
            ("shc@university.edu", "Committee SH", UserRole.SexualHarassmentCommittee, Gender.Female, "Password123!"),
            ("shc2@university.edu", "Prof. Nasrin Sultana", UserRole.SexualHarassmentCommittee, Gender.Female, "Password123!"),
            ("shc3@university.edu", "Prof. Iqbal Hossain", UserRole.SexualHarassmentCommittee, Gender.Male, "Password123!"),

            // Leadership
            ("vc@university.edu", "Vice Chancellor", UserRole.VC, Gender.Male, "Password123!"),
            ("admin@university.edu", "System Administrator", UserRole.SuperAdmin, Gender.Unspecified, "Admin@123!")
        };

        var existingEmails = context.Users.Select(u => u.Email).ToHashSet();
        var added = false;
        foreach (var (email, name, role, gender, password) in users)
        {
            if (existingEmails.Contains(email)) continue;
            context.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                Name = name,
                Role = role,
                Gender = gender,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            added = true;
        }

        if (added) await context.SaveChangesAsync();
    }

    public static async Task BackfillGendersAsync(ProctorDbContext context)
    {
        // Idempotently set Gender on previously-seeded users so the
        // female-coordinator auto-routing works on existing databases.
        var assignments = new Dictionary<string, Gender>
        {
            ["student@university.edu"] = Gender.Male,
            ["student2@university.edu"] = Gender.Female,
            ["student3@university.edu"] = Gender.Male,
            ["student4@university.edu"] = Gender.Female,
            ["student5@university.edu"] = Gender.Male,
            ["coordinator@university.edu"] = Gender.Female,
            ["coordinator2@university.edu"] = Gender.Male,
            ["fcoord@university.edu"] = Gender.Female,
            ["proctor@university.edu"] = Gender.Male,
            ["assistant@university.edu"] = Gender.Female,
            ["assistant2@university.edu"] = Gender.Male,
            ["assistant3@university.edu"] = Gender.Female,
            ["deputy@university.edu"] = Gender.Male,
            ["deputy2@university.edu"] = Gender.Male,
            ["deputy3@university.edu"] = Gender.Female,
            ["registrar@university.edu"] = Gender.Female,
            ["dc@university.edu"] = Gender.Male,
            ["shc@university.edu"] = Gender.Female,
            ["vc@university.edu"] = Gender.Male,
            ["admin@university.edu"] = Gender.Unspecified,
        };

        var changed = false;
        foreach (var u in context.Users.Where(u => u.Gender == Gender.Unspecified))
        {
            if (assignments.TryGetValue(u.Email, out var g) && g != Gender.Unspecified)
            {
                u.Gender = g;
                u.UpdatedAt = DateTime.UtcNow;
                changed = true;
            }
        }
        if (changed) await context.SaveChangesAsync();
    }
}
