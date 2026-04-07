using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Ranks;
using PROCTOR.Application.Interfaces;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Interfaces;

namespace PROCTOR.Application.Services;

public class RankService : IRankService
{
    private readonly IRepository<Rank> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public RankService(IRepository<Rank> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<List<RankDto>>> GetAllAsync()
    {
        var ranks = await _repository.GetAllAsync();
        var dtos = ranks.OrderBy(r => r.Order).Select(r => new RankDto
        {
            Id = r.Id.ToString(), Name = r.Name, Order = r.Order, IsActive = r.IsActive
        }).ToList();
        return ApiResponse<List<RankDto>>.SuccessResponse(dtos);
    }

    public async Task<ApiResponse<RankDto>> CreateAsync(CreateRankRequest request)
    {
        var rank = new Rank { Id = Guid.NewGuid(), Name = request.Name, Order = request.Order };
        await _repository.AddAsync(rank);
        await _unitOfWork.SaveChangesAsync();
        return ApiResponse<RankDto>.SuccessResponse(new RankDto
        {
            Id = rank.Id.ToString(), Name = rank.Name, Order = rank.Order, IsActive = rank.IsActive
        }, "Rank created.");
    }

    public async Task<ApiResponse<RankDto>> UpdateAsync(Guid id, UpdateRankRequest request)
    {
        var rank = await _repository.GetByIdAsync(id);
        if (rank is null) return ApiResponse<RankDto>.FailResponse("Rank not found.");
        if (request.Name is not null) rank.Name = request.Name;
        if (request.Order.HasValue) rank.Order = request.Order.Value;
        if (request.IsActive.HasValue) rank.IsActive = request.IsActive.Value;
        rank.UpdatedAt = DateTime.UtcNow;
        _repository.Update(rank);
        await _unitOfWork.SaveChangesAsync();
        return ApiResponse<RankDto>.SuccessResponse(new RankDto
        {
            Id = rank.Id.ToString(), Name = rank.Name, Order = rank.Order, IsActive = rank.IsActive
        }, "Rank updated.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
    {
        var rank = await _repository.GetByIdAsync(id);
        if (rank is null) return ApiResponse<bool>.FailResponse("Rank not found.");
        _repository.Remove(rank);
        await _unitOfWork.SaveChangesAsync();
        return ApiResponse<bool>.SuccessResponse(true, "Rank deleted.");
    }
}
