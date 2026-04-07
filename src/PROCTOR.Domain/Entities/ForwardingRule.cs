namespace PROCTOR.Domain.Entities;

public class ForwardingRule : BaseEntity
{
    public string FromRole { get; set; } = string.Empty;
    public string ToRole { get; set; } = string.Empty;
    public string? ResultStatus { get; set; }
    public bool IsActive { get; set; } = true;
}
