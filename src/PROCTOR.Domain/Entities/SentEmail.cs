namespace PROCTOR.Domain.Entities;

public class SentEmail : BaseEntity
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public Guid? RelatedCaseId { get; set; }
    public string Provider { get; set; } = "Demo";
}
