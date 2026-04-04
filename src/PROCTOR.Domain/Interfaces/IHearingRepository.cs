using PROCTOR.Domain.Entities;

namespace PROCTOR.Domain.Interfaces;

public interface IHearingRepository : IRepository<Hearing>
{
    Task<IEnumerable<Hearing>> GetByCaseIdAsync(Guid caseId);
    Task<Hearing?> GetByIdWithCaseAsync(Guid id);
}
