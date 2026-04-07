namespace PROCTOR.Application.DTOs.ForwardingRules;

public class ForwardingRuleDto
{
    public string Id { get; set; } = string.Empty;
    public string FromRole { get; set; } = string.Empty;
    public string ToRole { get; set; } = string.Empty;
    public string? ResultStatus { get; set; }
    public bool IsActive { get; set; }
}

public class CreateForwardingRuleRequest
{
    public string FromRole { get; set; } = string.Empty;
    public string ToRole { get; set; } = string.Empty;
    public string? ResultStatus { get; set; }
}

public class UpdateForwardingRuleRequest
{
    public string? ResultStatus { get; set; }
    public bool? IsActive { get; set; }
}
