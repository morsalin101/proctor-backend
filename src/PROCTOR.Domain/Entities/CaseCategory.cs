using PROCTOR.Domain.Enums;

namespace PROCTOR.Domain.Entities;

public class CaseCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public CaseCategoryAppliesTo AppliesToType { get; set; } = CaseCategoryAppliesTo.Both;
    public RiskSeverity RiskSeverity { get; set; } = RiskSeverity.Moderate;
    public int SortOrder { get; set; }

}
