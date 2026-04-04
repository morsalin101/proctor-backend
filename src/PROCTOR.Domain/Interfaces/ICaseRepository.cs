using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;

namespace PROCTOR.Domain.Interfaces;

public interface ICaseRepository : IRepository<Case>
{
    Task<Case?> GetByIdWithDetailsAsync(Guid id);
    Task<IEnumerable<Case>> GetFilteredAsync(CaseStatus? status, CaseType? type, Priority? priority, string? search, int page, int pageSize);
    Task<int> GetFilteredCountAsync(CaseStatus? status, CaseType? type, Priority? priority, string? search);
    Task<string> GenerateCaseNumberAsync();
}
