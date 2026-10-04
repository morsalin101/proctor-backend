using Microsoft.EntityFrameworkCore;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Seeders;

public static class CaseSeeder
{
    public static async Task SeedAsync(ProctorDbContext context)
    {
        // Check if cases are already seeded
        if (await context.Cases.AnyAsync()) return;

        var categories = await context.CaseCategories.Where(c => c.IsActive).ToListAsync();
        var studentUser = await context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Student);

        if (categories.Count == 0 || studentUser == null) return;

        var rand = new Random(42); // deterministic for reproducible seeds

        var firstNamesMale = new[] { "Rahim", "Karim", "Tariqul", "Tanvir", "Md. Al Amin", "Arafat", "Hasan", "Kamrul", "Sabbir", "Mehedi" };
        var firstNamesFemale = new[] { "Sadia", "Farhana", "Samiha", "Nusrat", "Tasnim", "Fariha", "Jannatul", "Sumaiya" };
        var lastNames = new[] { "Uddin", "Hossain", "Islam", "Rahman", "Ahmed", "Haque", "Akter", "Jahan", "Chowdhury", "Khan" };
        var departments = new[] { "CSE", "SWE", "BBA", "EEE", "TE", "Civil", "English", "Law" };

        var cases = new List<Case>();

        for (int i = 1; i <= 20; i++)
        {
            var isMale = rand.Next(2) == 0;
            var firstName = isMale ? firstNamesMale[rand.Next(firstNamesMale.Length)] : firstNamesFemale[rand.Next(firstNamesFemale.Length)];
            var lastName = lastNames[rand.Next(lastNames.Length)];
            var studentName = $"{firstName} {lastName}";
            
            var accusedIsMale = rand.Next(2) == 0;
            var accusedFirstName = accusedIsMale ? firstNamesMale[rand.Next(firstNamesMale.Length)] : firstNamesFemale[rand.Next(firstNamesFemale.Length)];
            var accusedLastName = lastNames[rand.Next(lastNames.Length)];
            var accusedName = $"{accusedFirstName} {accusedLastName}";

            var dept = departments[rand.Next(departments.Length)];
            var category = categories[rand.Next(categories.Count)];
            var type = i % 3 == 0 ? CaseType.Type1 : (i % 7 == 0 ? CaseType.Confidential : CaseType.Type2);

            var incidentDate = DateTime.UtcNow.AddDays(-rand.Next(1, 100));
            var term = incidentDate.Month switch
            {
                >= 1 and <= 4 => "Spring",
                >= 5 and <= 8 => "Summer",
                _ => "Fall"
            };

            var newCase = new Case
            {
                Id = Guid.NewGuid(),
                CaseNumber = $"DIU-{DateTime.UtcNow.Year}-{i.ToString("D4")}",
                StudentName = studentName,
                StudentId = $"0{rand.Next(1, 9)}{rand.Next(1, 3)}-{rand.Next(11, 40)}-{rand.Next(1000, 9999)}",
                Type = type,
                Status = CaseStatus.Submitted,
                Priority = type == CaseType.Type1 ? Priority.High : Priority.Medium,
                Description = $"This is a dummy case description for {studentName} regarding an incident in {dept} department.",
                SubmittedByUserId = studentUser.Id,
                SubmitterGender = isMale ? Gender.Male : Gender.Female,
                CategoryId = category.Id,
                IncidentDate = incidentDate,
                AcademicSemester = $"{term}-{incidentDate.Year}",
                StudentDepartment = dept,
                StudentSemester = rand.Next(1, 12),
                StudentCgpa = (decimal)(2.5 + rand.NextDouble() * 1.5),
                StudentContact = $"017{rand.Next(10000000, 99999999)}",
                AccusedName = accusedName,
                AccusedId = $"0{rand.Next(1, 9)}{rand.Next(1, 3)}-{rand.Next(11, 40)}-{rand.Next(1000, 9999)}",
                AccusedDepartment = departments[rand.Next(departments.Length)],
                AccusedContact = $"017{rand.Next(10000000, 99999999)}",
                CreatedAt = incidentDate.AddDays(1)
            };

            cases.Add(newCase);
        }

        await context.Cases.AddRangeAsync(cases);
        await context.SaveChangesAsync();
    }
}
