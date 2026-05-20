using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Seeders;

public static class CaseCategorySeeder
{
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
