using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Seeders;

/// <summary>
/// Seeds the real Proctorial Body of Daffodil International University.
/// Note: the <see cref="UserRole.Coordinator"/> role is presented as
/// "Assistant Administrative Officer" — see <see cref="RoleSeeder"/>.
/// </summary>
public static class ProctorialBodySeeder
{
    private const string DefaultPassword = "Password123!";

    private static readonly (string Email, string Name, string Rank, UserRole Role, Gender Gender)[] Members =
    [
        ("proctor@daffodilvarsity.edu.bd", "Dr. Shaikh Muhammad Allayear",
            "Professor and Proctor", UserRole.Proctor, Gender.Male),

        ("sharifa@daffodilvarsity.edu.bd", "Dr. Sharifa Sultana",
            "Professor and Deputy Proctor", UserRole.DeputyProctor, Gender.Female),

        ("badruzzaman.law@diu.edu.bd", "Mr. Mohammad Badruzzaman",
            "Assistant Professor & Deputy Proctor", UserRole.DeputyProctor, Gender.Male),

        ("diljeb@daffodilvarsity.edu.bd", "Mr. Kazi Md. Diljeb Kabir",
            "Deputy Director & Assistant Proctor", UserRole.AssistantProctor, Gender.Male),

        ("khalid@daffodilvarsity.edu.bd", "Mr. Khalid Been Badruzzaman Biplob",
            "Lecturer (Senior Scale) & Assistant Proctor", UserRole.AssistantProctor, Gender.Male),

        ("ahsan.law@diu.edu.bd", "Md. Ahsan Ullah",
            "Lecturer (Senior Scale) & Assistant Proctor", UserRole.AssistantProctor, Gender.Male),

        ("parvez.te@diu.edu.bd", "Md. Manik Parvez",
            "Lecturer & Assistant Proctor", UserRole.AssistantProctor, Gender.Male),

        ("jahangir.cse@diu.edu.bd", "Mohammad Jahangir Alam",
            "Assistant Professor & Assistant Proctor", UserRole.AssistantProctor, Gender.Male),

        ("proctoroffice@daffodilvarsity.edu.bd", "Md. Shariful Islam",
            "Assistant Administrative Officer", UserRole.Coordinator, Gender.Male),

        // Handles the female / confidential track (UserRole.FemaleCoordinator); her
        // designation in the Proctor Office is Assistant Administrative Officer.
        ("proctoroffice2@daffodilvarsity.edu.bd", "Nelima Afroz Oishee",
            "Assistant Administrative Officer", UserRole.FemaleCoordinator, Gender.Female),
    ];

    public static async Task SeedAsync(ProctorDbContext context)
    {
        var emails = Members.Select(m => m.Email).ToList();
        var existing = context.Users
            .Where(u => emails.Contains(u.Email))
            .ToDictionary(u => u.Email);

        var changed = false;
        foreach (var (email, name, rank, role, gender) in Members)
        {
            if (existing.TryGetValue(email, out var user))
            {
                // Keep designation/role details in sync without touching credentials.
                if (user.Name == name && user.RankName == rank && user.Role == role && user.Gender == gender)
                    continue;

                user.Name = name;
                user.RankName = rank;
                user.Role = role;
                user.Gender = gender;
                user.UpdatedAt = DateTime.UtcNow;
                changed = true;
                continue;
            }

            context.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                Name = name,
                RankName = rank,
                Role = role,
                Gender = gender,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            changed = true;
        }

        if (changed) await context.SaveChangesAsync();
    }
}
