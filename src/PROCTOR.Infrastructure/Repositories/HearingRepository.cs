using Microsoft.EntityFrameworkCore;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Interfaces;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Repositories;

public class HearingRepository : Repository<Hearing>, IHearingRepository
{
    public HearingRepository(ProctorDbContext context) : base(context) { }

    public async Task<IEnumerable<Hearing>> GetByCaseIdAsync(Guid caseId)
    {
        return await _dbSet
            .Where(h => h.CaseId == caseId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();
    }

    public async Task<Hearing?> GetByIdWithCaseAsync(Guid id)
    {
        return await _dbSet
            .Include(h => h.Case)
            .FirstOrDefaultAsync(h => h.Id == id);
    }
}
