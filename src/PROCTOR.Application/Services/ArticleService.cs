using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Articles;
using PROCTOR.Application.Interfaces;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Interfaces;

namespace PROCTOR.Application.Services;

public class ArticleService : IArticleService
{
    private readonly IRepository<Article> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ArticleService(IRepository<Article> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    private static string Sanitize(string? input) =>
        input?.Replace("\0", string.Empty).Trim() ?? string.Empty;

    public async Task<ApiResponse<List<ArticleDto>>> GetAllAsync()
    {
        var articles = await _repository.GetAllAsync();
        var dtos = articles.OrderBy(a => a.Order).Select(a => new ArticleDto
        {
            Id = a.Id.ToString(),
            ArticleNo = a.ArticleNo,
            Title = a.Title,
            Description = a.Description,
            IsActive = a.IsActive,
            Order = a.Order
        }).ToList();
        return ApiResponse<List<ArticleDto>>.SuccessResponse(dtos);
    }

    public async Task<ApiResponse<ArticleDto>> CreateAsync(CreateArticleRequest request)
    {
        var article = new Article
        {
            Id = Guid.NewGuid(),
            ArticleNo = Sanitize(request.ArticleNo),
            Title = Sanitize(request.Title),
            Description = Sanitize(request.Description),
            Order = request.Order
        };
        await _repository.AddAsync(article);
        await _unitOfWork.SaveChangesAsync();
        return ApiResponse<ArticleDto>.SuccessResponse(new ArticleDto
        {
            Id = article.Id.ToString(), ArticleNo = article.ArticleNo, Title = article.Title,
            Description = article.Description, IsActive = article.IsActive, Order = article.Order
        }, "Article created.");
    }

    public async Task<ApiResponse<ArticleDto>> UpdateAsync(Guid id, UpdateArticleRequest request)
    {
        var article = await _repository.GetByIdAsync(id);
        if (article is null) return ApiResponse<ArticleDto>.FailResponse("Article not found.");
        if (request.ArticleNo is not null) article.ArticleNo = Sanitize(request.ArticleNo);
        if (request.Title is not null) article.Title = Sanitize(request.Title);
        if (request.Description is not null) article.Description = Sanitize(request.Description);
        if (request.Order.HasValue) article.Order = request.Order.Value;
        if (request.IsActive.HasValue) article.IsActive = request.IsActive.Value;
        article.UpdatedAt = DateTime.UtcNow;
        _repository.Update(article);
        await _unitOfWork.SaveChangesAsync();
        return ApiResponse<ArticleDto>.SuccessResponse(new ArticleDto
        {
            Id = article.Id.ToString(), ArticleNo = article.ArticleNo, Title = article.Title,
            Description = article.Description, IsActive = article.IsActive, Order = article.Order
        }, "Article updated.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
    {
        var article = await _repository.GetByIdAsync(id);
        if (article is null) return ApiResponse<bool>.FailResponse("Article not found.");
        _repository.Remove(article);
        await _unitOfWork.SaveChangesAsync();
        return ApiResponse<bool>.SuccessResponse(true, "Article deleted.");
    }
}
