namespace PROCTOR.Domain.Entities;

public class Article : BaseEntity
{
    public string ArticleNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int Order { get; set; }
}
