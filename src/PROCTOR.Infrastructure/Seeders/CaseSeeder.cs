using Microsoft.EntityFrameworkCore;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Seeders;

public static class CaseSeeder
{
    public static async Task SeedAsync(ProctorDbContext context)
    {
        // Check if Bangla cases are already seeded
        if (await context.Cases.AnyAsync(c => c.Description.Contains("ঘটেছে"))) return;

        var categories = await context.CaseCategories.Where(c => c.IsActive).ToListAsync();
        var studentUser = await context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Student);

        if (categories.Count == 0 || studentUser == null) return;

        var rand = new Random(42); // deterministic for reproducible seeds

        var firstNamesMale = new[] { "রহিম", "করিম", "তরিকুল", "তানভীর", "মোঃ আল আমিন", "আরাফাত", "হাসান", "কামরুল", "সাব্বির", "মেহেদী" };
        var firstNamesFemale = new[] { "সাদিয়া", "ফারহানা", "সামিহা", "নুসরাত", "তাসনিম", "ফারিহা", "জান্নাতুল", "সুমাইয়া" };
        var lastNames = new[] { "উদ্দিন", "হোসেন", "ইসলাম", "রহমান", "আহমেদ", "হক", "আক্তার", "জাহান", "চৌধুরী", "খান" };
        var departments = new[] { "সিএসই", "সফটওয়্যার ইঞ্জিনিয়ারিং", "বিবিএ", "ত্রিপল-ই", "টেক্সটাইল", "সিভিল", "ইংরেজি", "আইন" };

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
                CaseNumber = $"DIU-{DateTime.UtcNow.Year}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                StudentName = studentName,
                StudentId = $"0{rand.Next(1, 9)}{rand.Next(1, 3)}-{rand.Next(11, 40)}-{rand.Next(1000, 9999)}",
                Type = type,
                Status = CaseStatus.Submitted,
                Priority = type == CaseType.Type1 ? Priority.High : Priority.Medium,
                Description = $"এটি {dept} বিভাগে {studentName} এর সাথে ঘটে যাওয়া একটি ঘটনার ডামি কেস। ঘটনাটি সম্পর্কে বিস্তারিত তদন্তের জন্য অভিযোগটি দাখিল করা হলো।",
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
