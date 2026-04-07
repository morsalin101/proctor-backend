using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Ranks;

namespace PROCTOR.Application.Interfaces;

public interface IRankService
{
    Task<ApiResponse<List<RankDto>>> GetAllAsync();
    Task<ApiResponse<RankDto>> CreateAsync(CreateRankRequest request);
    Task<ApiResponse<RankDto>> UpdateAsync(Guid id, UpdateRankRequest request);
    Task<ApiResponse<bool>> DeleteAsync(Guid id);
}
