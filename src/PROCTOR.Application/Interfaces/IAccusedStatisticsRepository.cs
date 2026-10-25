using PROCTOR.Application.DTOs.AccusedStatistics;

namespace PROCTOR.Application.Interfaces;

public interface IAccusedStatisticsRepository
{
    Task<AccusedStatisticsDto> GetAsync(AccusedStatisticsFilter filter, string role, Guid? userId);
}
