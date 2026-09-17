namespace PROCTOR.Application.DTOs.Dashboard;

public class DashboardFilter
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Type { get; set; }
    public string? Department { get; set; }
    public Guid? SubjectId { get; set; }
    public Guid? CategoryId { get; set; }
    public string? ResponsibleRole { get; set; }
    public Guid? ResponsiblePersonId { get; set; }
    public int? Year { get; set; }
    public int? Semester { get; set; }
    public decimal? MinCgpa { get; set; }
    public decimal? MaxCgpa { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class DashboardAnalyticsDto
{
    public int TotalCases { get; set; }
    public int OpenCases { get; set; }
    public int ResolvedCases { get; set; }
    public int PendingCases { get; set; }
    public int UnderReview { get; set; }
    public int Type1Pending { get; set; }
    public int Type2Pending { get; set; }
    public List<DashboardGroupDto> MonthlyTrend { get; set; } = [];
    public List<DashboardGroupDto> YearlyTrend { get; set; } = [];
    public List<DashboardGroupDto> Semesters { get; set; } = [];
    public List<DashboardGroupDto> CaseTypes { get; set; } = [];
    public List<DashboardGroupDto> Categories { get; set; } = [];
    public List<DashboardGroupDto> CgpaRanges { get; set; } = [];
    public List<DashboardGroupDto> Workload { get; set; } = [];
    public List<DashboardGroupDto> RoleWorkload { get; set; } = [];
    public List<string> Departments { get; set; } = [];
    public List<DashboardPersonDto> People { get; set; } = [];
    public List<DashboardCaseDto> Cases { get; set; } = [];
    public List<DashboardActivityDto> Activity { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public record DashboardGroupDto(string Name, int Count);
public record DashboardPersonDto(Guid Id, string Name, string Role);
public record DashboardCaseDto(Guid Id, string CaseNumber, string StudentName, string StudentId,
    string? Department, string? CategoryName, int? Semester, decimal? Cgpa, string Status, string Type,
    string? AssignedTo, DateTime CreatedAt);
public record DashboardActivityDto(Guid CaseId, string CaseNumber, string Action,
    string User, DateTime Timestamp);
