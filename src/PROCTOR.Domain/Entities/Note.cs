namespace PROCTOR.Domain.Entities;

public class Note : BaseEntity
{
    public Guid CaseId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;

    public Case Case { get; set; } = null!;
}
