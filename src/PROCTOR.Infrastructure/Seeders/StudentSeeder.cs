using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Seeders;

public static class StudentSeeder
{
    public static async Task SeedAsync(ProctorDbContext context)
    {
        var students = new (string StudentId, string Name, string Dept, string Contact, Gender Gender, string Father, string FatherContact, string Advisor, decimal Cgpa)[]
        {
            ("123", "Rahim Uddin",    "CSE",  "01710000123", Gender.Male,   "Karim Uddin",   "01810000123", "Dr. Salam", 3.42m),
            ("124", "Karima Akter",   "EEE",  "01710000124", Gender.Female, "Abdul Karim",   "01810000124", "Dr. Nasrin", 3.67m),
            ("125", "Sabbir Ahmed",   "BBA",  "01710000125", Gender.Male,   "Jasim Uddin",   "01810000125", "Dr. Haque", 3.15m),
            ("126", "Tania Sultana",  "CSE",  "01710000126", Gender.Female, "Mizanur Rahman","01810000126", "Dr. Salam", 3.81m),
            ("127", "Imran Hossain",  "Civil","01710000127", Gender.Male,   "Anwar Hossain", "01810000127", "Dr. Kabir", 3.29m),
            ("128", "Nusrat Jahan",   "Law",  "01710000128", Gender.Female, "Shahidul Islam","01810000128", "Dr. Roksana", 3.73m),
            ("129", "Fahim Reza",     "ME",   "01710000129", Gender.Male,   "Golam Reza",    "01810000129", "Dr. Mizan", 3.56m),
            ("130", "Sadia Islam",    "Pharmacy","01710000130", Gender.Female,"Nurul Islam",  "01810000130", "Dr. Farida", 3.90m),
        };

        var existing = context.Students.ToDictionary(s => s.StudentId);
        var existingByEmail = context.Students
            .Where(s => s.Email != null)
            .GroupBy(s => s.Email!.ToLower())
            .ToDictionary(group => group.Key, group => group.First());
        var changed = false;
        foreach (var (studentId, name, dept, contact, gender, father, fatherContact, advisor, cgpa) in students)
        {
            if (existing.TryGetValue(studentId, out var record))
            {
                if (record.Cgpa is null)
                {
                    record.Cgpa = cgpa;
                    record.UpdatedAt = DateTime.UtcNow;
                    changed = true;
                }
                continue;
            }
            var student = new Student
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                Name = name,
                Department = dept,
                Contact = contact,
                Email = $"{studentId}@university.edu",
                Gender = gender,
                Cgpa = cgpa,
                FatherName = father,
                FatherContact = fatherContact,
                AdvisorName = advisor,
                GuardianContact = fatherContact,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Students.Add(student);
            existing[studentId] = student;
            changed = true;
        }

        // Match the quick-login student accounts by email so their profile and
        // incident form can resolve a real student directory record.
        var demoStudents = new (string Id, string Name, string Email, string Dept, Gender Gender, decimal Cgpa)[]
        {
            ("2026001", "John Student", "student@university.edu", "CSE", Gender.Male, 3.62m),
            ("2026002", "Aisha Rahman", "student2@university.edu", "EEE", Gender.Female, 3.84m),
            ("2026003", "Tariq Hossain", "student3@university.edu", "BBA", Gender.Male, 3.27m),
            ("2026004", "Nadia Akter", "student4@university.edu", "CSE", Gender.Female, 3.75m),
            ("2026005", "Rafiq Ahmed", "student5@university.edu", "Civil", Gender.Male, 3.44m),
            ("2026006", "Mehedi Hassan", "student6@university.edu", "ME", Gender.Male, 3.18m),
            ("2026007", "Fatima Karim", "student7@university.edu", "Law", Gender.Female, 3.91m),
            ("2026008", "Sumaiya Islam", "student8@university.edu", "Pharmacy", Gender.Female, 3.68m),
        };

        foreach (var (id, name, email, dept, gender, cgpa) in demoStudents)
        {
            if (existingByEmail.TryGetValue(email, out var linkedRecord))
            {
                if (linkedRecord.Cgpa is null)
                {
                    linkedRecord.Cgpa = cgpa;
                    linkedRecord.UpdatedAt = DateTime.UtcNow;
                    changed = true;
                }
                continue;
            }

            if (existing.TryGetValue(id, out var record))
            {
                // Keep an existing student's ID and data intact if it clashes
                // with this demo ID; never link their record to another account.
                continue;
            }

            var student = new Student
            {
                Id = Guid.NewGuid(),
                StudentId = id,
                Name = name,
                Email = email,
                Department = dept,
                Gender = gender,
                Cgpa = cgpa,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Students.Add(student);
            existing[id] = student;
            existingByEmail[email] = student;
            changed = true;
        }

        if (changed) await context.SaveChangesAsync();
    }
}
