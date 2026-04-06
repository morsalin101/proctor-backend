using Microsoft.EntityFrameworkCore;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Domain.Interfaces;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Repositories;

public class CaseRepository : Repository<Case>, ICaseRepository
{
    public CaseRepository(ProctorDbContext context) : base(context) { }

    public async Task<Case?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _dbSet
            .Include(c => c.Documents)
            .Include(c => c.Notes)
            .Include(c => c.Hearings)
            .Include(c => c.TimelineEvents)
            .Include(c => c.AssignedTo)
            .Include(c => c.Reports)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Case>> GetFilteredAsync(
        CaseStatus? status,
        CaseType? type,
        Priority? priority,
        string? search,
        int page,
        int pageSize)
    {
        var query = ApplyFilters(status, type, priority, search);

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(
        CaseStatus? status,
        CaseType? type,
        Priority? priority,
        string? search)
    {
        var query = ApplyFilters(status, type, priority, search);
        return await query.CountAsync();
    }

    public async Task<string> GenerateCaseNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"CASE-{year}-";
        var lastCase = await _dbSet
            .Where(c => c.CaseNumber.StartsWith(prefix))
            .OrderByDescending(c => c.CaseNumber)
            .FirstOrDefaultAsync();

        var sequence = 1;
        if (lastCase is not null)
        {
            var lastNum = lastCase.CaseNumber.Replace(prefix, "");
            if (int.TryParse(lastNum, out var parsed))
                sequence = parsed + 1;
        }

        return $"{prefix}{sequence:D3}";
    }

    private IQueryable<Case> ApplyFilters(
        CaseStatus? status,
        CaseType? type,
        Priority? priority,
        string? search)
    {
        var query = _dbSet.AsQueryable();

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        if (type.HasValue)
            query = query.Where(c => c.Type == type.Value);

        if (priority.HasValue)
            query = query.Where(c => c.Priority == priority.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(c =>
                c.CaseNumber.ToLower().Contains(searchLower) ||
                c.StudentName.ToLower().Contains(searchLower) ||
                c.Description.ToLower().Contains(searchLower));
        }

        return query;
    }
}
