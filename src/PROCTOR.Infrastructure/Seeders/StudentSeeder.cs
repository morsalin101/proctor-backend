using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Seeders;

public static class StudentSeeder
{
    public static async Task SeedAsync(ProctorDbContext context)
    {
        var students = new (string StudentId, string Name, string Dept, string Contact, Gender Gender, string Father, string FatherContact, string Advisor)[]
        {
            ("123", "Rahim Uddin",    "CSE",  "01710000123", Gender.Male,   "Karim Uddin",   "01810000123", "Dr. Salam"),
            ("124", "Karima Akter",   "EEE",  "01710000124", Gender.Female, "Abdul Karim",   "01810000124", "Dr. Nasrin"),
            ("125", "Sabbir Ahmed",   "BBA",  "01710000125", Gender.Male,   "Jasim Uddin",   "01810000125", "Dr. Haque"),
            ("126", "Tania Sultana",  "CSE",  "01710000126", Gender.Female, "Mizanur Rahman","01810000126", "Dr. Salam"),
            ("127", "Imran Hossain",  "Civil","01710000127", Gender.Male,   "Anwar Hossain", "01810000127", "Dr. Kabir"),
            ("128", "Nusrat Jahan",   "Law",  "01710000128", Gender.Female, "Shahidul Islam","01810000128", "Dr. Roksana"),
            ("129", "Fahim Reza",     "ME",   "01710000129", Gender.Male,   "Golam Reza",    "01810000129", "Dr. Mizan"),
            ("130", "Sadia Islam",    "Pharmacy","01710000130", Gender.Female,"Nurul Islam",  "01810000130", "Dr. Farida"),
        };

        var existingIds = context.Students.Select(s => s.StudentId).ToHashSet();
        var added = false;
        foreach (var (studentId, name, dept, contact, gender, father, fatherContact, advisor) in students)
        {
            if (existingIds.Contains(studentId)) continue;
            context.Students.Add(new Student
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                Name = name,
                Department = dept,
                Contact = contact,
                Email = $"{studentId}@university.edu",
                Gender = gender,
                FatherName = father,
                FatherContact = fatherContact,
                AdvisorName = advisor,
                GuardianContact = fatherContact,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            added = true;
        }

        if (added) await context.SaveChangesAsync();
    }
}
