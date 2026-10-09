namespace PROCTOR.Domain.Entities;

public class ForwardingRule : BaseEntity
{
    public string FromRole { get; set; } = string.Empty;
    public string ToRole { get; set; } = string.Empty;
    /// <summary>
    /// The case track this rule applies to. Confidential cases use the Type-2 track.
    /// </summary>
    public string AppliesToType { get; set; } = "type-2";
    public string? ResultStatus { get; set; }
    public bool IsActive { get; set; } = true;
}
