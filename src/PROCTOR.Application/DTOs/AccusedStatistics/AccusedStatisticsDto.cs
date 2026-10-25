namespace PROCTOR.Application.DTOs.AccusedStatistics;

public class AccusedStatisticsFilter
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Department { get; set; }
    public string? Batch { get; set; }
    public string? Gender { get; set; }
    public Guid? CategoryId { get; set; }
}

public class AccusedStatisticsDto
{
    public DateTime GeneratedAt { get; set; }
    public DateTime? PeriodFrom { get; set; }
    public DateTime? PeriodTo { get; set; }
    public AccusedStatisticsSummaryDto Summary { get; set; } = new();
    public AccusedDataCoverageDto DataCoverage { get; set; } = new();
    public List<AccusedYearTrendDto> YearlyTrend { get; set; } = [];
    public List<AccusedYearCategoryDto> ComplaintYearlyTrend { get; set; } = [];
    public List<AccusedYearCategoryDto> MmsYearlyTrend { get; set; } = [];
    public List<AccusedComplaintDto> Complaints { get; set; } = [];
    public List<AccusedRiskGroupDto> MmsDistribution { get; set; } = [];
    public List<AccusedBreakdownDto> MmsProfiles { get; set; } = [];
    public List<AccusedCrossTabDto> MmsByRisk { get; set; } = [];
    public List<AccusedRiskGroupDto> AcademicRisk { get; set; } = [];
    public List<AccusedRiskGroupDto> FinancialRisk { get; set; } = [];
    public List<AccusedRiskGroupDto> CcrRisk { get; set; } = [];
    public List<AccusedRiskGroupDto> DropoutRisk { get; set; } = [];
    public List<AccusedRiskGroupDto> CgpaDistribution { get; set; } = [];
    public List<AccusedBreakdownDto> Departments { get; set; } = [];
    public List<AccusedBreakdownDto> Batches { get; set; } = [];
    public List<AccusedBreakdownDto> Genders { get; set; } = [];
    public List<AccusedCrossTabDto> ComplaintByMms { get; set; } = [];
    public List<AccusedCrossTabDto> ComplaintByRisk { get; set; } = [];
    public List<AccusedCrossTabDto> ComplaintByAcademicRisk { get; set; } = [];
    public List<AccusedCrossTabDto> ComplaintByFinancialRisk { get; set; } = [];
    public AccusedHighRiskProfileDto HighRiskProfile { get; set; } = new();
    public List<AccusedRiskGroupDto> HighRiskComplaints { get; set; } = [];
    public List<AccusedRiskGroupDto> HighRiskDepartments { get; set; } = [];
    public List<AccusedRiskGroupDto> HighRiskMmsDistribution { get; set; } = [];
    public List<AccusedYearCountDto> HighRiskYearlyTrend { get; set; } = [];
    public List<DepartmentComplaintDto> TopDepartmentComplaints { get; set; } = [];
}

public class AccusedStatisticsSummaryDto
{
    public int TotalRecords { get; set; }
    public int UniqueStudents { get; set; }
    public int RepeatOffenders { get; set; }
    public int RedZone { get; set; }
    public decimal RedZoneRate { get; set; }
    public int HighAcademicRisk { get; set; }
    public int HighFinancialRisk { get; set; }
    public int HighDropoutRisk { get; set; }
    public int PoorPoor { get; set; }
    public decimal? AverageCgpa { get; set; }
    public decimal? MedianCgpa { get; set; }
    public decimal? AverageDues { get; set; }
}

public class AccusedDataCoverageDto
{
    public int TotalRecords { get; set; }
    public int MatchedStudents { get; set; }
    public int MissingStudentId { get; set; }
    public int UnmatchedStudentId { get; set; }
    public int MissingCgpa { get; set; }
    public int MissingDues { get; set; }
    public int MissingCredits { get; set; }
    public int MissingGender { get; set; }
    public int MissingDepartment { get; set; }
    public int MissingBatch { get; set; }
}

public record AccusedYearTrendDto(int Year, int Total, int RedZone, decimal RedZoneRate, decimal? AverageCgpa);
public record AccusedYearCategoryDto(int Year, List<AccusedCrossTabValueDto> Values);
public record AccusedYearCountDto(int Year, int Count);
public record AccusedRiskGroupDto(string Name, int Count, decimal Percentage);
public record AccusedBreakdownDto(
    string Name,
    int Count,
    int RedZone,
    decimal RedZoneRate,
    int HighAcademicRisk,
    int HighFinancialRisk,
    int HighDropoutRisk,
    int PoorPoor,
    decimal? AverageCgpa,
    decimal? AverageDues);
public record AccusedCrossTabDto(string Name, List<AccusedCrossTabValueDto> Values);
public record AccusedCrossTabValueDto(string Name, int Count);
public record DepartmentComplaintDto(int Rank, string Department, string Complaint, int Count, decimal Percentage);

public class AccusedComplaintDto
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
    public decimal? AverageCgpa { get; set; }
    public decimal? AverageDues { get; set; }
    public int RedZone { get; set; }
    public decimal RedZoneRate { get; set; }
    public int HighAcademicRisk { get; set; }
    public int HighFinancialRisk { get; set; }
    public int HighDropoutRisk { get; set; }
    public int PoorPoor { get; set; }
    public int Male { get; set; }
    public int Female { get; set; }
    public int BelowMmsCgpa { get; set; }
    public int FinanciallyPoor { get; set; }
    public int AnyHighRisk { get; set; }
    public decimal MaleRate { get; set; }
    public decimal FemaleRate { get; set; }
    public decimal BelowMmsCgpaRate { get; set; }
    public decimal FinanciallyPoorRate { get; set; }
    public decimal AnyHighRiskRate { get; set; }
    public decimal? AverageRiskScore { get; set; }
    public decimal? MedianRiskScore { get; set; }
    public decimal? RedZoneAverageCgpa { get; set; }
    public decimal? NonRedZoneAverageCgpa { get; set; }
    public decimal? CgpaGap { get; set; }
    public decimal? RedZoneAverageDues { get; set; }
    public decimal? NonRedZoneAverageDues { get; set; }
    public string TopDepartments { get; set; } = string.Empty;
}

public class AccusedHighRiskProfileDto
{
    public int Count { get; set; }
    public int Male { get; set; }
    public int Female { get; set; }
    public decimal? AverageCgpa { get; set; }
    public decimal? AverageDues { get; set; }
    public string? TopComplaint { get; set; }
    public string? TopDepartment { get; set; }
}
