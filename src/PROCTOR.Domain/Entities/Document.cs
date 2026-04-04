using PROCTOR.Domain.Enums;

namespace PROCTOR.Domain.Entities;

public class Document : BaseEntity
{
    public Guid CaseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DocumentType Type { get; set; }
    public string Url { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;

    public Case Case { get; set; } = null!;
}
