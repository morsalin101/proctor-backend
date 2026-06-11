using PROCTOR.Domain.Entities;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Seeders;

public static class CaseSubjectSeeder
{
    public static async Task SeedAsync(ProctorDbContext context)
    {
        if (context.CaseSubjects.Any()) return;

        var subjects = new[]
        {
            "Ragging / Bullying",
            "Examination Malpractice / Cheating",
            "Physical Assault",
            "Verbal Abuse / Harassment",
            "Sexual Harassment",
            "Property Damage / Vandalism",
            "Theft",
            "Drug / Substance Abuse",
            "Misconduct in Hall / Dormitory",
            "Cyberbullying / Online Harassment",
            "Plagiarism",
            "Disruption of Academic Activities",
        };

        var order = 0;
        foreach (var s in subjects)
        {
            context.CaseSubjects.Add(new CaseSubject
            {
                Id = Guid.NewGuid(),
                Subject = s,
                Order = order++,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
    }
}
