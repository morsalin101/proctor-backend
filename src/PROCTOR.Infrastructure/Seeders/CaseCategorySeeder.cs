using Microsoft.EntityFrameworkCore;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Seeders;

public static class CaseCategorySeeder
{
    /// <summary>Add mapped Bangla choices to both new and existing installations.</summary>
    public static async Task SeedBanglaAndMapAsync(ProctorDbContext context)
    {
        const string seedKey = "bangla_case_taxonomy_seed_v1";
        if (await context.SystemSettings.AnyAsync(s => s.Key == seedKey)) return;

        var choices = new (string Subject, string Category, string Description, bool Confidential)[]
        {
            ("র‍্যাগিং ও বুলিং", "র‍্যাগিং", "নবীন বা অন্য শিক্ষার্থীকে র‍্যাগিং করা।", false),
            ("র‍্যাগিং ও বুলিং", "বুলিং", "শিক্ষার্থীকে ভয় দেখানো বা অপমান করা।", false),
            ("পরীক্ষায় অনিয়ম", "পরীক্ষায় নকল", "পরীক্ষায় অসদুপায় অবলম্বন করা।", false),
            ("শারীরিক ও মৌখিক নির্যাতন", "মারামারি", "শারীরিক আক্রমণ বা মারামারি।", false),
            ("শারীরিক ও মৌখিক নির্যাতন", "হুমকি ও গালিগালাজ", "হুমকি, গালিগালাজ বা মৌখিক হয়রানি।", false),
            ("যৌন হয়রানি", "যৌন হয়রানি", "যৌন হয়রানি সংক্রান্ত অভিযোগ।", true),
            ("যৌন হয়রানি", "অনলাইনে যৌন হয়রানি", "অনলাইনে যৌন হয়রানি সংক্রান্ত অভিযোগ।", true),
            ("চুরি ও সম্পত্তির ক্ষতি", "চুরি", "চুরি সংক্রান্ত অভিযোগ।", false),
            ("চুরি ও সম্পত্তির ক্ষতি", "সম্পত্তির ক্ষতি", "বিশ্ববিদ্যালয়ের বা ব্যক্তিগত সম্পত্তির ক্ষতি।", false),
            ("মাদক ও নিষিদ্ধ দ্রব্য", "মাদক ব্যবহার", "মাদক বা নিষিদ্ধ দ্রব্য ব্যবহার সংক্রান্ত অভিযোগ।", true),
        };

        var subjects = await context.CaseSubjects.ToListAsync();
        var categories = await context.CaseCategories.ToListAsync();
        var now = DateTime.UtcNow;
        var subjectOrder = Math.Max(100, subjects.Select(s => s.Order).DefaultIfEmpty(0).Max() + 1);
        var categoryOrder = Math.Max(100, categories.Select(c => c.SortOrder).DefaultIfEmpty(0).Max() + 1);

        foreach (var choice in choices)
        {
            var subject = subjects.FirstOrDefault(s => string.Equals(s.Subject, choice.Subject, StringComparison.OrdinalIgnoreCase));
            if (subject is null)
            {
                subject = new CaseSubject { Id = Guid.NewGuid(), Subject = choice.Subject,
                    Order = subjectOrder++, IsActive = true, CreatedAt = now, UpdatedAt = now };
                context.CaseSubjects.Add(subject);
                subjects.Add(subject);
            }

            var category = categories.FirstOrDefault(c => string.Equals(c.Name, choice.Category, StringComparison.OrdinalIgnoreCase));
            if (category is null)
            {
                category = new CaseCategory { Id = Guid.NewGuid(), Name = choice.Category,
                    Description = choice.Description, IsConfidential = choice.Confidential,
                    AppliesToType = CaseCategoryAppliesTo.Type2, SubjectId = subject.Id,
                    SortOrder = categoryOrder++, IsActive = true, CreatedAt = now, UpdatedAt = now };
                context.CaseCategories.Add(category);
                categories.Add(category);
            }
            else if (category.SubjectId is null)
            {
                category.SubjectId = subject.Id;
                category.UpdatedAt = now;
            }
        }

        // Existing English defaults also need a subject link for the Type-2 form.
        var englishMappings = new Dictionary<string, string>
        {
            ["Ragging"] = "Ragging / Bullying",
            ["Cheating"] = "Examination Malpractice / Cheating",
            ["Misconduct"] = "Misconduct in Hall / Dormitory",
            ["Property Damage"] = "Property Damage / Vandalism",
            ["Harassment"] = "Sexual Harassment",
            ["Substance Abuse"] = "Drug / Substance Abuse",
        };
        foreach (var (categoryName, subjectName) in englishMappings)
        {
            var category = categories.FirstOrDefault(c => c.Name == categoryName && c.SubjectId is null);
            var subject = subjects.FirstOrDefault(s => s.Subject == subjectName);
            if (category is null || subject is null) continue;
            category.SubjectId = subject.Id;
            category.UpdatedAt = now;
        }

        context.SystemSettings.Add(new SystemSetting
        {
            Id = Guid.NewGuid(), Key = seedKey, Value = "true", Category = "maintenance",
            Description = "Bangla case subjects and mapped categories seeded.",
            CreatedAt = now, UpdatedAt = now
        });
        await context.SaveChangesAsync();
    }

    public static async Task SeedAsync(ProctorDbContext context)
    {
        if (context.CaseCategories.Any()) return;

        var items = new (string Name, string? Description, bool IsConfidential, CaseCategoryAppliesTo AppliesTo, int Sort)[]
        {
            ("Ragging", "Hazing and intimidation of juniors.", false, CaseCategoryAppliesTo.Both, 1),
            ("Cheating", "Academic dishonesty during examinations or assignments.", false, CaseCategoryAppliesTo.Type2, 2),
            ("Misconduct", "General disciplinary misconduct.", false, CaseCategoryAppliesTo.Both, 3),
            ("Property Damage", "Damage to campus property or facilities.", false, CaseCategoryAppliesTo.Both, 4),
            ("Harassment", "Sexual harassment or abuse complaints.", true, CaseCategoryAppliesTo.Type2, 5),
            ("Substance Abuse", "Possession or use of prohibited substances.", true, CaseCategoryAppliesTo.Both, 6),
            ("Other", "Any other incident or grievance.", false, CaseCategoryAppliesTo.Both, 99),
        };

        foreach (var (name, desc, confidential, appliesTo, sort) in items)
        {
            context.CaseCategories.Add(new CaseCategory
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = desc,
                IsConfidential = confidential,
                IsActive = true,
                AppliesToType = appliesTo,
                SortOrder = sort,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }

        await context.SaveChangesAsync();
    }
}
