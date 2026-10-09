namespace PROCTOR.Domain.Enums;

public enum CaseType
{
    Type1,
    Type2,
    // Kept for backward-compatible request parsing. Persisted confidential cases are
    // migrated to Type2 + Case.IsConfidential.
    Confidential,
    Type3
}
