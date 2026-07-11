using PROCTOR.Domain.Enums;

namespace PROCTOR.Domain.Entities;

public class CaseCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsConfidential { get; set; }
    public bool IsActive { get; set; } = true;
    public CaseCategoryAppliesTo AppliesToType { get; set; } = CaseCategoryAppliesTo.Both;
    public int SortOrder { get; set; }

    /// <summary>
    /// Optional owning subject. A subject has many categories; on the Type-2 form the student
    /// picks a subject first and the category dropdown filters to that subject's categories.
    /// </summary>
    public Guid? SubjectId { get; set; }
    public CaseSubject? Subject { get; set; }
}
