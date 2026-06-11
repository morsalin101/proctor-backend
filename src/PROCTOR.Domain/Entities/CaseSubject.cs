namespace PROCTOR.Domain.Entities;

/// <summary>
/// A predefined Type-2 case subject. Admin-managed; suggested to the student on the
/// Type-2 form's "Case Subject" field as they type.
/// </summary>
public class CaseSubject : BaseEntity
{
    public string Subject { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
}
