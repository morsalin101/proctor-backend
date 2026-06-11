using PROCTOR.Domain.Enums;

namespace PROCTOR.Domain.Entities;

/// <summary>
/// Student master record. Used to auto-fill complainant/accused details on the
/// Type-2 case form (looked up by StudentId) and to power the Students directory.
/// </summary>
public class Student : BaseEntity
{
    public string StudentId { get; set; } = string.Empty; // university roll, e.g. "123"
    public string Name { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Contact { get; set; }
    public string? Email { get; set; }
    public Gender Gender { get; set; } = Gender.Unspecified;
    public string? AdvisorName { get; set; }
    public string? FatherName { get; set; }
    public string? FatherContact { get; set; }
    public string? GuardianContact { get; set; }
    public bool IsActive { get; set; } = true;
}
