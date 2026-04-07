namespace PROCTOR.Application.DTOs.Articles;

public class ArticleDto
{
    public string Id { get; set; } = string.Empty;
    public string ArticleNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int Order { get; set; }
}

public class CreateArticleRequest
{
    public string ArticleNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class UpdateArticleRequest
{
    public string? ArticleNo { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? Order { get; set; }
    public bool? IsActive { get; set; }
}
