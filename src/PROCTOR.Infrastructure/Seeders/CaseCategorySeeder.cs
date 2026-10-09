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

        var choices = new (string Category, string Description)[]
        {
            ("র‍্যাগিং", "নবীন বা অন্য শিক্ষার্থীকে র‍্যাগিং করা।"),
            ("বুলিং", "শিক্ষার্থীকে ভয় দেখানো বা অপমান করা।"),
            ("পরীক্ষায় নকল", "পরীক্ষায় অসদুপায় অবলম্বন করা।"),
            ("মারামারি", "শারীরিক আক্রমণ বা মারামারি।"),
            ("হুমকি ও গালিগালাজ", "হুমকি, গালিগালাজ বা মৌখিক হয়রানি।"),
            ("যৌন হয়রানি", "যৌন হয়রানি সংক্রান্ত অভিযোগ।"),
            ("অনলাইনে যৌন হয়রানি", "অনলাইনে যৌন হয়রানি সংক্রান্ত অভিযোগ।"),
            ("চুরি", "চুরি সংক্রান্ত অভিযোগ।"),
            ("সম্পত্তির ক্ষতি", "বিশ্ববিদ্যালয়ের বা ব্যক্তিগত সম্পত্তির ক্ষতি।"),
            ("মাদক ব্যবহার", "মাদক বা নিষিদ্ধ দ্রব্য ব্যবহার সংক্রান্ত অভিযোগ।"),
        };

        var categories = await context.CaseCategories.ToListAsync();
        var now = DateTime.UtcNow;
        var categoryOrder = Math.Max(100, categories.Select(c => c.SortOrder).DefaultIfEmpty(0).Max() + 1);

        foreach (var choice in choices)
        {
            var category = categories.FirstOrDefault(c => string.Equals(c.Name, choice.Category, StringComparison.OrdinalIgnoreCase));
            if (category is null)
            {
                category = new CaseCategory { Id = Guid.NewGuid(), Name = choice.Category,
                    Description = choice.Description,
                    AppliesToType = CaseCategoryAppliesTo.Type2,
                    SortOrder = categoryOrder++, IsActive = true, CreatedAt = now, UpdatedAt = now };
                context.CaseCategories.Add(category);
                categories.Add(category);
            }
        }

        context.SystemSettings.Add(new SystemSetting
        {
            Id = Guid.NewGuid(), Key = seedKey, Value = "true", Category = "maintenance",
            Description = "Bangla case categories seeded.",
            CreatedAt = now, UpdatedAt = now
        });
        await context.SaveChangesAsync();
    }

    public static async Task SeedAsync(ProctorDbContext context)
    {
        if (context.CaseCategories.Any()) return;

        var items = new (string Name, string? Description, CaseCategoryAppliesTo AppliesTo, int Sort)[]
        {
            ("Ragging", "Hazing and intimidation of juniors.", CaseCategoryAppliesTo.Both, 1),
            ("Cheating", "Academic dishonesty during examinations or assignments.", CaseCategoryAppliesTo.Type2, 2),
            ("Misconduct", "General disciplinary misconduct.", CaseCategoryAppliesTo.Both, 3),
            ("Property Damage", "Damage to campus property or facilities.", CaseCategoryAppliesTo.Both, 4),
            ("Harassment", "Sexual harassment or abuse complaints.", CaseCategoryAppliesTo.Type2, 5),
            ("Substance Abuse", "Possession or use of prohibited substances.", CaseCategoryAppliesTo.Both, 6),
            ("Other", "Any other incident or grievance.", CaseCategoryAppliesTo.Both, 99),
        };

        foreach (var (name, desc, appliesTo, sort) in items)
        {
            context.CaseCategories.Add(new CaseCategory
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = desc,
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
