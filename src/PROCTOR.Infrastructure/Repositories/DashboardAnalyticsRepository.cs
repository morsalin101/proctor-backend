using Microsoft.EntityFrameworkCore;
using PROCTOR.Application.DTOs.Dashboard;
using PROCTOR.Application.Interfaces;
using PROCTOR.Application.Mapping;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Repositories;

public class DashboardAnalyticsRepository : IDashboardAnalyticsRepository
{
    private readonly ProctorDbContext _db;

    public DashboardAnalyticsRepository(ProctorDbContext db) => _db = db;

    public async Task<DashboardAnalyticsDto> GetAsync(DashboardFilter filter, string role, Guid? userId)
    {
        var source = _db.Cases.AsNoTracking().AsQueryable();
        if (role == "student")
            source = source.Where(c => userId.HasValue && c.SubmittedByUserId == userId.Value);
        else if (role == "female-coordinator")
            source = source.Where(c => c.Type == CaseType.Confidential || c.Type == CaseType.Type1 || c.SubmitterGender == Gender.Female);
        else if (role != "proctor" && role != "coordinator" && role != "super-admin" && role != "vc")
            source = source.Where(c =>
                (c.ForwardedToRole == role || (userId.HasValue &&
                    (c.AssignedToId == userId.Value || c.Assignments.Any(a => a.IsActive && a.UserId == userId.Value))))
                && (c.Type != CaseType.Confidential || role == "sexual-harassment-committee"));

        var departments = await source.Where(c => c.StudentDepartment != null && c.StudentDepartment != "")
            .Select(c => c.StudentDepartment!).Distinct().OrderBy(x => x).ToListAsync();
        var people = await _db.Users.AsNoTracking().Where(u => u.IsActive && u.Role != UserRole.Student && u.Role != UserRole.External)
            .Select(u => new { u.Id, u.Name, u.Role }).OrderBy(u => u.Name).ToListAsync();

        var query = source;
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(c => EF.Functions.ILike(c.CaseNumber, $"%{term}%")
                || EF.Functions.ILike(c.StudentName, $"%{term}%")
                || EF.Functions.ILike(c.StudentId, $"%{term}%")
                || EF.Functions.ILike(c.Description, $"%{term}%")
                || (c.AccusedName != null && EF.Functions.ILike(c.AccusedName, $"%{term}%"))
                || c.AccusedPersons.Any(a => EF.Functions.ILike(a.Name, $"%{term}%") || EF.Functions.ILike(a.AccusedStudentId, $"%{term}%")));
        }
        if (!string.IsNullOrWhiteSpace(filter.Status) && TryEnum<CaseStatus>(filter.Status, out var status))
            query = query.Where(c => c.Status == status);
        if (!string.IsNullOrWhiteSpace(filter.Type) && TryEnum<CaseType>(filter.Type, out var type))
            query = query.Where(c => c.Type == type);
        if (!string.IsNullOrWhiteSpace(filter.Department))
            query = query.Where(c => c.StudentDepartment == filter.Department);
        if (filter.SubjectId.HasValue)
        {
            var subjectId = filter.SubjectId.Value;
            var subjectName = await _db.CaseSubjects.AsNoTracking()
                .Where(s => s.Id == subjectId).Select(s => s.Subject).FirstOrDefaultAsync();
            query = subjectName is null
                ? query.Where(c => false)
                : query.Where(c => (c.Category != null && c.Category.SubjectId == subjectId) || c.Subject == subjectName);
        }
        if (filter.CategoryId.HasValue)
            query = query.Where(c => c.CategoryId == filter.CategoryId.Value);
        if (filter.Year.HasValue)
            query = query.Where(c => c.CreatedAt.Year == filter.Year.Value);
        if (filter.Semester.HasValue)
            query = query.Where(c => c.StudentSemester == filter.Semester.Value);
        if (filter.MinCgpa.HasValue)
            query = query.Where(c => c.StudentCgpa >= filter.MinCgpa.Value);
        if (filter.MaxCgpa.HasValue)
            query = query.Where(c => c.StudentCgpa <= filter.MaxCgpa.Value);
        if (filter.From.HasValue)
        {
            var start = DateTime.SpecifyKind(filter.From.Value.Date, DateTimeKind.Utc);
            query = query.Where(c => c.CreatedAt >= start);
        }
        if (filter.To.HasValue)
        {
            var end = DateTime.SpecifyKind(filter.To.Value.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(c => c.CreatedAt < end);
        }
        if (filter.ResponsiblePersonId.HasValue)
        {
            var personId = filter.ResponsiblePersonId.Value;
            query = query.Where(c => c.AssignedToId == personId || c.Assignments.Any(a => a.IsActive && a.UserId == personId));
        }
        if (!string.IsNullOrWhiteSpace(filter.ResponsibleRole) && TryEnum<UserRole>(filter.ResponsibleRole, out var responsibleRole))
            query = query.Where(c => c.Assignments.Any(a => a.IsActive && a.User != null && a.User.Role == responsibleRole)
                || (c.AssignedTo != null && c.AssignedTo.Role == responsibleRole)
                || c.ForwardedToRole == filter.ResponsibleRole);

        var count = await query.CountAsync();
        var open = query.Where(c => c.Status != CaseStatus.Resolved && c.Status != CaseStatus.Closed && c.Status != CaseStatus.Rejected);
        var resolved = query.Where(c => c.Status == CaseStatus.Resolved || c.Status == CaseStatus.Closed);
        var monthRows = await query.GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .OrderBy(g => g.Year).ThenBy(g => g.Month).ToListAsync();
        var yearRows = await query.GroupBy(c => c.CreatedAt.Year)
            .Select(g => new { Year = g.Key, Count = g.Count() }).OrderBy(g => g.Year).ToListAsync();
        var semesterRows = await query.GroupBy(c => c.StudentSemester)
            .Select(g => new { Semester = g.Key, Count = g.Count() }).OrderBy(g => g.Semester).ToListAsync();
        var typeRows = await query.GroupBy(c => c.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() }).ToListAsync();
        var categoryRows = await query.GroupBy(c => c.Category != null ? c.Category.Name : "Uncategorized")
            .Select(g => new { Name = g.Key, Count = g.Count() }).OrderByDescending(g => g.Count).ToListAsync();
        var roleRows = await query.GroupBy(c => c.ForwardedToRole)
            .Select(g => new { Role = g.Key, Count = g.Count() }).ToListAsync();
        var cgpaRows = await query.GroupBy(c => c.StudentCgpa == null ? 0 :
            c.StudentCgpa < 2 ? 1 : c.StudentCgpa < 3 ? 2 : c.StudentCgpa < 3.5m ? 3 : 4)
            .Select(g => new { Bucket = g.Key, Count = g.Count() }).ToListAsync();

        // A case with several assignees contributes once to each person's workload.
        var activeAssignments = _db.CaseAssignments.AsNoTracking()
            .Where(a => a.IsActive && query.Any(c => c.Id == a.CaseId))
            .Select(a => new { a.UserId, a.CaseId });
        var legacyAssignments = query.Where(c => c.AssignedToId != null)
            .Select(c => new { UserId = c.AssignedToId!.Value, CaseId = c.Id });
        var workloadRows = await activeAssignments.Union(legacyAssignments)
            .GroupBy(a => a.UserId).Select(g => new { UserId = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count).Take(12).ToListAsync();

        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 50);
        var cases = await query.OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new { c.Id, c.CaseNumber, c.StudentName, c.StudentId, c.StudentDepartment,
                CategoryName = c.Category != null ? c.Category.Name : null,
                c.StudentSemester, c.StudentCgpa, c.Status, c.Type, AssignedTo = c.AssignedTo != null ? c.AssignedTo.Name : null, c.CreatedAt })
            .ToListAsync();
        var activity = await _db.TimelineEvents.AsNoTracking()
            .Where(e => query.Any(c => c.Id == e.CaseId))
            .OrderByDescending(e => e.CreatedAt).Take(8)
            .Select(e => new { e.CaseId, e.Case.CaseNumber, e.Action, e.User, e.CreatedAt }).ToListAsync();

        return new DashboardAnalyticsDto
        {
            TotalCases = count,
            OpenCases = await open.CountAsync(),
            ResolvedCases = await resolved.CountAsync(),
            PendingCases = await query.CountAsync(c => c.Status == CaseStatus.Submitted || c.Status == CaseStatus.Pending || c.Status == CaseStatus.ResubmissionRequested),
            UnderReview = await query.CountAsync(c => c.Status == CaseStatus.UnderReview || c.Status == CaseStatus.Verified || c.Status == CaseStatus.Assigned),
            Type1Pending = await open.CountAsync(c => c.Type == CaseType.Type1),
            Type2Pending = await open.CountAsync(c => c.Type == CaseType.Type2 || c.Type == CaseType.Confidential),
            MonthlyTrend = monthRows.Select(x => new DashboardGroupDto($"{x.Year}-{x.Month:00}", x.Count)).ToList(),
            YearlyTrend = yearRows.Select(x => new DashboardGroupDto(x.Year.ToString(), x.Count)).ToList(),
            Semesters = semesterRows.Select(x => new DashboardGroupDto(x.Semester?.ToString() ?? "Unknown", x.Count)).ToList(),
            CaseTypes = typeRows.Select(x => new DashboardGroupDto(x.Type.ToKebabCase(), x.Count)).ToList(),
            Categories = categoryRows.Select(x => new DashboardGroupDto(x.Name ?? "Uncategorized", x.Count)).ToList(),
            CgpaRanges = cgpaRows.Select(x => new DashboardGroupDto(x.Bucket switch
                { 1 => "Below 2.00", 2 => "2.00–2.99", 3 => "3.00–3.49", 4 => "3.50–4.00", _ => "Unknown" }, x.Count)).ToList(),
            Workload = workloadRows.Select(x => new DashboardGroupDto(people.FirstOrDefault(p => p.Id == x.UserId)?.Name ?? "Unknown", x.Count)).ToList(),
            RoleWorkload = roleRows.Select(x => new DashboardGroupDto(x.Role ?? "Unrouted", x.Count)).ToList(),
            Departments = departments,
            People = people.Select(p => new DashboardPersonDto(p.Id, p.Name, p.Role.ToKebabCase())).ToList(),
            Cases = cases.Select(c => new DashboardCaseDto(c.Id, c.CaseNumber, c.StudentName, c.StudentId,
                c.StudentDepartment, c.CategoryName, c.StudentSemester, c.StudentCgpa, c.Status.ToKebabCase(), c.Type.ToKebabCase(), c.AssignedTo, c.CreatedAt)).ToList(),
            Activity = activity.Select(e => new DashboardActivityDto(e.CaseId, e.CaseNumber, e.Action, e.User, e.CreatedAt)).ToList(),
            Page = page,
            PageSize = pageSize
        };
    }

    private static bool TryEnum<T>(string input, out T value) where T : struct, Enum =>
        Enum.TryParse(input.Replace("-", ""), true, out value);
}
