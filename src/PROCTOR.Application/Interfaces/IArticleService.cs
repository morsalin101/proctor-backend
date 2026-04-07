using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Articles;

namespace PROCTOR.Application.Interfaces;

public interface IArticleService
{
    Task<ApiResponse<List<ArticleDto>>> GetAllAsync();
    Task<ApiResponse<ArticleDto>> CreateAsync(CreateArticleRequest request);
    Task<ApiResponse<ArticleDto>> UpdateAsync(Guid id, UpdateArticleRequest request);
    Task<ApiResponse<bool>> DeleteAsync(Guid id);
}
