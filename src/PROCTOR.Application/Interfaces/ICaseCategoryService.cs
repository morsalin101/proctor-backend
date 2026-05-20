using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.CaseCategories;

namespace PROCTOR.Application.Interfaces;

public interface ICaseCategoryService
{
    Task<ApiResponse<List<CaseCategoryDto>>> GetAllAsync(bool includeInactive = false);
    Task<ApiResponse<CaseCategoryDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<CaseCategoryDto>> CreateAsync(CreateCaseCategoryRequest request);
    Task<ApiResponse<CaseCategoryDto>> UpdateAsync(Guid id, UpdateCaseCategoryRequest request);
    Task<ApiResponse<bool>> DeleteAsync(Guid id);
}
