using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.CaseCategories;
using PROCTOR.Application.Interfaces;
using PROCTOR.Application.Mapping;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Domain.Interfaces;

namespace PROCTOR.Application.Services;

public class CaseCategoryService : ICaseCategoryService
{
    private readonly IRepository<CaseCategory> _repo;
    private readonly IUnitOfWork _unitOfWork;

    public CaseCategoryService(IRepository<CaseCategory> repo, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<List<CaseCategoryDto>>> GetAllAsync(bool includeInactive = false)
    {
        var items = await _repo.GetAllAsync();
        var filtered = includeInactive ? items : items.Where(x => x.IsActive);
        var dtos = filtered
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => c.ToDto())
            .ToList();
        return ApiResponse<List<CaseCategoryDto>>.SuccessResponse(dtos);
    }

    public async Task<ApiResponse<CaseCategoryDto>> GetByIdAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return ApiResponse<CaseCategoryDto>.FailResponse("Category not found.");
        return ApiResponse<CaseCategoryDto>.SuccessResponse(entity.ToDto());
    }

    public async Task<ApiResponse<CaseCategoryDto>> CreateAsync(CreateCaseCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return ApiResponse<CaseCategoryDto>.FailResponse("Name is required.");

        var entity = new CaseCategory
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description,
            IsConfidential = request.IsConfidential,
            IsActive = request.IsActive,
            AppliesToType = MappingExtensions.ParseEnum<CaseCategoryAppliesTo>(request.AppliesToType),
            SortOrder = request.SortOrder
        };

        await _repo.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<CaseCategoryDto>.SuccessResponse(entity.ToDto(), "Category created.");
    }

    public async Task<ApiResponse<CaseCategoryDto>> UpdateAsync(Guid id, UpdateCaseCategoryRequest request)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return ApiResponse<CaseCategoryDto>.FailResponse("Category not found.");

        if (request.Name is not null) entity.Name = request.Name.Trim();
        if (request.Description is not null) entity.Description = request.Description;
        if (request.IsConfidential.HasValue) entity.IsConfidential = request.IsConfidential.Value;
        if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
        if (request.AppliesToType is not null)
            entity.AppliesToType = MappingExtensions.ParseEnum<CaseCategoryAppliesTo>(request.AppliesToType);
        if (request.SortOrder.HasValue) entity.SortOrder = request.SortOrder.Value;

        _repo.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<CaseCategoryDto>.SuccessResponse(entity.ToDto(), "Category updated.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return ApiResponse<bool>.FailResponse("Category not found.");

        // Soft delete to preserve historical case references
        entity.IsActive = false;
        _repo.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true, "Category deactivated.");
    }
}
