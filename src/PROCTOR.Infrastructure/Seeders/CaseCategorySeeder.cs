using Microsoft.EntityFrameworkCore;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Seeders;

public static class CaseCategorySeeder
{
    public static async Task SeedRiskSeveritiesAsync(ProctorDbContext context)
    {
        const string flagKey = "case_category_risk_severity_v1";
        if (await context.SystemSettings.AnyAsync(x => x.Key == flagKey && x.Value == "true")) return;
        var categories = await context.CaseCategories.ToListAsync();
        var highTerms = new[] { "harassment", "substance", "drug", "theft", "fight", "মাদক", "চুরি", "মারামারি", "হয়রানি", "হয়রানি" };
        var lowTerms = new[] { "other", "অন্যান্য" };
        foreach (var category in categories)
        {
            category.RiskSeverity = highTerms.Any(term => category.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                ? RiskSeverity.High
                : lowTerms.Any(term => category.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                    ? RiskSeverity.Low : RiskSeverity.Moderate;
        }
        context.SystemSettings.Add(new SystemSetting
        {
            Id = Guid.NewGuid(), Key = flagKey, Value = "true", Category = "internal",
            Description = "Guard flag: case-category risk severities were seeded once."
        });
        await context.SaveChangesAsync();
    }

    /// <summary>Add mapped Bangla choices to both new and existing installations.</summary>
    public static async Task SeedBanglaAndMapAsync(ProctorDbContext context)
    {
        const string seedKey = "bangla_case_taxonomy_seed_v1";
        if (await context.SystemSettings.AnyAsync(s => s.Key == seedKey)) return;

        var choices = new (string Category, string Description, RiskSeverity Severity)[]
        {
            ("র‍্যাগিং", "নবীন বা অন্য শিক্ষার্থীকে র‍্যাগিং করা।", RiskSeverity.Moderate),
            ("বুলিং", "শিক্ষার্থীকে ভয় দেখানো বা অপমান করা।", RiskSeverity.Moderate),
            ("পরীক্ষায় নকল", "পরীক্ষায় অসদুপায় অবলম্বন করা।", RiskSeverity.Moderate),
            ("মারামারি", "শারীরিক আক্রমণ বা মারামারি।", RiskSeverity.High),
            ("হুমকি ও গালিগালাজ", "হুমকি, গালিগালাজ বা মৌখিক হয়রানি।", RiskSeverity.High),
            ("যৌন হয়রানি", "যৌন হয়রানি সংক্রান্ত অভিযোগ।", RiskSeverity.High),
            ("অনলাইনে যৌন হয়রানি", "অনলাইনে যৌন হয়রানি সংক্রান্ত অভিযোগ।", RiskSeverity.High),
            ("চুরি", "চুরি সংক্রান্ত অভিযোগ।", RiskSeverity.High),
            ("সম্পত্তির ক্ষতি", "বিশ্ববিদ্যালয়ের বা ব্যক্তিগত সম্পত্তির ক্ষতি।", RiskSeverity.Moderate),
            ("মাদক ব্যবহার", "মাদক বা নিষিদ্ধ দ্রব্য ব্যবহার সংক্রান্ত অভিযোগ।", RiskSeverity.High),
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
                    AppliesToType = CaseCategoryAppliesTo.Type2, RiskSeverity = choice.Severity,
                    SortOrder = categoryOrder++, IsActive = true, CreatedAt = now, UpdatedAt = now };
                context.CaseCategories.Add(category);
                categories.Add(category);
            }
            else
            {
                category.RiskSeverity = choice.Severity;
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
                RiskSeverity = name is "Substance Abuse" or "Harassment" ? RiskSeverity.High
                    : name is "Other" ? RiskSeverity.Low : RiskSeverity.Moderate,
                SortOrder = sort,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }

        await context.SaveChangesAsync();
    }
}
