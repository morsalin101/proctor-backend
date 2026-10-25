using Microsoft.EntityFrameworkCore;
using PROCTOR.Application.DTOs.AccusedStatistics;
using PROCTOR.Application.Interfaces;
using PROCTOR.Application.Mapping;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Repositories;

public class AccusedStatisticsRepository : IAccusedStatisticsRepository
{
    private const decimal MmsCgpaBoundary = 2.90m;
    private const decimal FinancialBoundary = 35_000m;
    private readonly ProctorDbContext _db;

    public AccusedStatisticsRepository(ProctorDbContext db) => _db = db;

    public async Task<AccusedStatisticsDto> GetAsync(AccusedStatisticsFilter filter, string role, Guid? userId)
    {
        var query = VisibleCases(role, userId)
            .Include(c => c.Category)
            .Include(c => c.AccusedPersons)
            .AsNoTracking();

        if (filter.From.HasValue)
        {
            var from = DateTime.SpecifyKind(filter.From.Value.Date, DateTimeKind.Utc);
            query = query.Where(c => (c.IncidentDate ?? c.CreatedAt) >= from);
        }
        if (filter.To.HasValue)
        {
            var to = DateTime.SpecifyKind(filter.To.Value.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(c => (c.IncidentDate ?? c.CreatedAt) < to);
        }
        if (filter.CategoryId.HasValue)
            query = query.Where(c => c.CategoryId == filter.CategoryId.Value);

        var cases = await query.OrderBy(c => c.CreatedAt).ToListAsync();
        var students = await _db.Students.AsNoTracking().ToListAsync();
        var studentsById = students
            .Where(s => !string.IsNullOrWhiteSpace(s.StudentId))
            .GroupBy(s => NormalizeId(s.StudentId), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var rows = new List<RiskRow>();
        foreach (var caseItem in cases)
        {
            var accused = caseItem.AccusedPersons.OrderBy(a => a.Order).ToList();
            if (accused.Count > 0)
            {
                rows.AddRange(accused.Select(person => BuildRow(caseItem, person, studentsById)));
            }
            else if (HasLegacyAccused(caseItem))
            {
                rows.Add(BuildRow(caseItem, null, studentsById));
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.Department))
            rows = rows.Where(r => string.Equals(r.Department, filter.Department.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(filter.Batch))
            rows = rows.Where(r => string.Equals(r.Batch, filter.Batch.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(filter.Gender))
            rows = rows.Where(r => string.Equals(r.Gender, filter.Gender.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

        return BuildResponse(rows);
    }

    private static RiskRow BuildRow(Case caseItem, CaseAccused? accused, IReadOnlyDictionary<string, Student> studentsById)
    {
        var rawStudentId = accused?.AccusedStudentId ?? caseItem.AccusedId ?? string.Empty;
        var studentId = NormalizeId(rawStudentId);
        studentsById.TryGetValue(studentId, out var student);
        var cgpa = accused?.Cgpa ?? student?.Cgpa;
        var dues = student?.OutstandingDues;
        var academicRisk = AcademicRisk(cgpa);
        var financialRisk = FinancialRisk(student);
        var ccrRisk = CcrRisk(student?.CompletedCredits, student?.ExpectedCredits);
        var mms = Mms(cgpa, dues);
        var riskScore = DropoutScore(academicRisk, financialRisk, ccrRisk, mms, student?.RegistrationStatus, caseItem.Category?.RiskSeverity);
        var dropout = DropoutRisk(riskScore);
        var redZone = IsRedZone(academicRisk, financialRisk, ccrRisk, dropout, student?.RegistrationStatus);

        return new RiskRow
        {
            Date = caseItem.IncidentDate ?? caseItem.CreatedAt,
            StudentId = studentId,
            StudentMatched = student is not null,
            Complaint = caseItem.Category?.Name ?? "Uncategorized",
            Department = accused?.Department ?? student?.Department,
            Batch = student?.Batch,
            Gender = student?.Gender == Gender.Unspecified ? null : student?.Gender.ToKebabCase(),
            Cgpa = cgpa,
            Dues = dues,
            CompletedCredits = student?.CompletedCredits,
            ExpectedCredits = student?.ExpectedCredits,
            AcademicRisk = academicRisk,
            FinancialRisk = financialRisk,
            CcrRisk = ccrRisk,
            Mms = mms,
            RiskScore = riskScore,
            DropoutRisk = dropout,
            RedZone = redZone
        };
    }

    private static AccusedStatisticsDto BuildResponse(List<RiskRow> rows)
    {
        var total = rows.Count;
        var ids = rows.Where(r => r.StudentId.Length > 0).GroupBy(r => r.StudentId, StringComparer.OrdinalIgnoreCase).ToList();
        var repeatOffenders = ids.Count(g => g.Count() > 1);
        var redZone = rows.Count(r => r.RedZone);
        var highRisk = rows.Where(r => r.DropoutRisk is "High" or "Very High").ToList();

        return new AccusedStatisticsDto
        {
            GeneratedAt = DateTime.UtcNow,
            PeriodFrom = rows.Count == 0 ? null : rows.Min(r => r.Date),
            PeriodTo = rows.Count == 0 ? null : rows.Max(r => r.Date),
            Summary = new AccusedStatisticsSummaryDto
            {
                TotalRecords = total,
                UniqueStudents = ids.Count,
                RepeatOffenders = repeatOffenders,
                RedZone = redZone,
                RedZoneRate = Percent(redZone, total),
                HighAcademicRisk = rows.Count(r => r.AcademicRisk is "High" or "Severe"),
                HighFinancialRisk = rows.Count(r => r.FinancialRisk is "High" or "Severe"),
                HighDropoutRisk = highRisk.Count,
                PoorPoor = rows.Count(r => r.Mms == "Poor–Poor"),
                AverageCgpa = Average(rows.Select(r => r.Cgpa)),
                MedianCgpa = Median(rows.Select(r => r.Cgpa)),
                AverageDues = Average(rows.Select(r => r.Dues))
            },
            DataCoverage = new AccusedDataCoverageDto
            {
                TotalRecords = total,
                MatchedStudents = rows.Count(r => r.StudentMatched),
                MissingStudentId = rows.Count(r => r.StudentId.Length == 0),
                UnmatchedStudentId = rows.Count(r => r.StudentId.Length > 0 && !r.StudentMatched),
                MissingCgpa = rows.Count(r => !r.Cgpa.HasValue),
                MissingDues = rows.Count(r => !r.Dues.HasValue),
                MissingCredits = rows.Count(r => !r.CompletedCredits.HasValue || !r.ExpectedCredits.HasValue || r.ExpectedCredits <= 0),
                MissingGender = rows.Count(r => string.IsNullOrWhiteSpace(r.Gender)),
                MissingDepartment = rows.Count(r => string.IsNullOrWhiteSpace(r.Department)),
                MissingBatch = rows.Count(r => string.IsNullOrWhiteSpace(r.Batch))
            },
            YearlyTrend = rows.GroupBy(r => r.Date.Year).OrderBy(g => g.Key)
                .Select(g => new AccusedYearTrendDto(g.Key, g.Count(), g.Count(r => r.RedZone), Percent(g.Count(r => r.RedZone), g.Count()), Average(g.Select(r => r.Cgpa))))
                .ToList(),
            ComplaintYearlyTrend = rows.GroupBy(r => r.Date.Year).OrderBy(g => g.Key)
                .Select(g => new AccusedYearCategoryDto(g.Key, g.GroupBy(r => r.Complaint)
                    .OrderByDescending(x => x.Count()).ThenBy(x => x.Key)
                    .Select(x => new AccusedCrossTabValueDto(x.Key, x.Count())).ToList()))
                .ToList(),
            MmsYearlyTrend = rows.GroupBy(r => r.Date.Year).OrderBy(g => g.Key)
                .Select(g => new AccusedYearCategoryDto(g.Key, g.GroupBy(r => r.Mms)
                    .OrderBy(x => x.Key)
                    .Select(x => new AccusedCrossTabValueDto(x.Key, x.Count())).ToList()))
                .ToList(),
            Complaints = rows.GroupBy(r => r.Complaint).OrderByDescending(g => g.Count()).ThenBy(g => g.Key)
                .Select(g => Complaint(g.Key, g.ToList(), total)).ToList(),
            MmsDistribution = RiskGroups(rows, r => r.Mms, ["Fit–Fit", "Poor–Fit", "Fit–Poor", "Poor–Poor", "Unknown"]),
            MmsProfiles = OrderedBreakdown(rows, r => r.Mms, ["Fit–Fit", "Fit–Poor", "Poor–Fit", "Poor–Poor", "Unknown"]),
            MmsByRisk = CrossTab(rows, r => r.Mms, r => r.DropoutRisk),
            AcademicRisk = RiskGroups(rows, r => r.AcademicRisk, ["Low", "Moderate", "High", "Severe", "Unknown"]),
            FinancialRisk = RiskGroups(rows, r => r.FinancialRisk, ["Low", "Moderate", "High", "Severe", "Unknown"]),
            CcrRisk = RiskGroups(rows, r => r.CcrRisk, ["Low", "Moderate", "High", "Severe", "Unknown"]),
            DropoutRisk = RiskGroups(rows, r => r.DropoutRisk, ["Low", "Moderate", "High", "Very High", "Unknown"]),
            CgpaDistribution = RiskGroups(rows, CgpaBucket, ["Below 2.50", "2.50–2.89", "2.90–2.99", "3.00–3.49", "3.50–4.00", "Unknown"]),
            Departments = Breakdown(rows, r => r.Department),
            Batches = Breakdown(rows, r => r.Batch),
            Genders = Breakdown(rows, r => r.Gender),
            ComplaintByMms = CrossTab(rows, r => r.Complaint, r => r.Mms),
            ComplaintByRisk = CrossTab(rows, r => r.Complaint, r => r.DropoutRisk),
            ComplaintByAcademicRisk = CrossTab(rows, r => r.Complaint, r => r.AcademicRisk),
            ComplaintByFinancialRisk = CrossTab(rows, r => r.Complaint, r => r.FinancialRisk),
            HighRiskProfile = new AccusedHighRiskProfileDto
            {
                Count = highRisk.Count,
                Male = highRisk.Count(r => r.Gender == "male"),
                Female = highRisk.Count(r => r.Gender == "female"),
                AverageCgpa = Average(highRisk.Select(r => r.Cgpa)),
                AverageDues = Average(highRisk.Select(r => r.Dues)),
                TopComplaint = TopName(highRisk, r => r.Complaint),
                TopDepartment = TopName(highRisk, r => r.Department)
            },
            HighRiskComplaints = DynamicRiskGroups(highRisk, r => r.Complaint),
            HighRiskDepartments = DynamicRiskGroups(highRisk, r => string.IsNullOrWhiteSpace(r.Department) ? "Unknown" : r.Department!),
            HighRiskMmsDistribution = RiskGroups(highRisk, r => r.Mms, ["Fit–Fit", "Poor–Fit", "Fit–Poor", "Poor–Poor", "Unknown"]),
            HighRiskYearlyTrend = highRisk.GroupBy(r => r.Date.Year).OrderBy(g => g.Key).Select(g => new AccusedYearCountDto(g.Key, g.Count())).ToList(),
            TopDepartmentComplaints = rows.Where(r => !string.IsNullOrWhiteSpace(r.Department))
                .GroupBy(r => new { Department = r.Department!, r.Complaint })
                .OrderByDescending(g => g.Count()).ThenBy(g => g.Key.Department).ThenBy(g => g.Key.Complaint)
                .Take(20).Select((g, index) => new DepartmentComplaintDto(index + 1, g.Key.Department, g.Key.Complaint, g.Count(), Percent(g.Count(), total))).ToList()
        };
    }

    private static AccusedComplaintDto Complaint(string name, List<RiskRow> rows, int total)
    {
        var redZone = rows.Count(r => r.RedZone);
        return new AccusedComplaintDto
        {
            Name = name,
            Count = rows.Count,
            Percentage = Percent(rows.Count, total),
            AverageCgpa = Average(rows.Select(r => r.Cgpa)),
            AverageDues = Average(rows.Select(r => r.Dues)),
            RedZone = redZone,
            RedZoneRate = Percent(redZone, rows.Count),
            HighAcademicRisk = rows.Count(r => r.AcademicRisk is "High" or "Severe"),
            HighFinancialRisk = rows.Count(r => r.FinancialRisk is "High" or "Severe"),
            HighDropoutRisk = rows.Count(r => r.DropoutRisk is "High" or "Very High"),
            PoorPoor = rows.Count(r => r.Mms == "Poor–Poor"),
            Male = rows.Count(r => r.Gender == "male"),
            Female = rows.Count(r => r.Gender == "female"),
            BelowMmsCgpa = rows.Count(r => r.Cgpa.HasValue && r.Cgpa < MmsCgpaBoundary),
            FinanciallyPoor = rows.Count(r => r.Dues.HasValue && r.Dues > FinancialBoundary),
            AnyHighRisk = rows.Count(r => r.AcademicRisk is "High" or "Severe"
                || r.FinancialRisk is "High" or "Severe" || r.CcrRisk is "High" or "Severe"
                || r.DropoutRisk is "High" or "Very High"),
            MaleRate = Percent(rows.Count(r => r.Gender == "male"), rows.Count),
            FemaleRate = Percent(rows.Count(r => r.Gender == "female"), rows.Count),
            BelowMmsCgpaRate = Percent(rows.Count(r => r.Cgpa.HasValue && r.Cgpa < MmsCgpaBoundary), rows.Count(r => r.Cgpa.HasValue)),
            FinanciallyPoorRate = Percent(rows.Count(r => r.Dues.HasValue && r.Dues > FinancialBoundary), rows.Count(r => r.Dues.HasValue)),
            AnyHighRiskRate = Percent(rows.Count(r => r.AcademicRisk is "High" or "Severe"
                || r.FinancialRisk is "High" or "Severe" || r.CcrRisk is "High" or "Severe"
                || r.DropoutRisk is "High" or "Very High"), rows.Count),
            AverageRiskScore = Average(rows.Select(r => r.RiskScore.HasValue ? (decimal?)r.RiskScore.Value : null)),
            MedianRiskScore = Median(rows.Select(r => r.RiskScore.HasValue ? (decimal?)r.RiskScore.Value : null)),
            RedZoneAverageCgpa = Average(rows.Where(r => r.RedZone).Select(r => r.Cgpa)),
            NonRedZoneAverageCgpa = Average(rows.Where(r => !r.RedZone).Select(r => r.Cgpa)),
            CgpaGap = CgpaGap(rows),
            RedZoneAverageDues = Average(rows.Where(r => r.RedZone).Select(r => r.Dues)),
            NonRedZoneAverageDues = Average(rows.Where(r => !r.RedZone).Select(r => r.Dues)),
            TopDepartments = string.Join(", ", rows.Where(r => !string.IsNullOrWhiteSpace(r.Department))
                .GroupBy(r => r.Department!).OrderByDescending(g => g.Count()).ThenBy(g => g.Key)
                .Take(2).Select(g => $"{g.Key}: {g.Count()}"))
        };
    }

    private static List<AccusedRiskGroupDto> RiskGroups(List<RiskRow> rows, Func<RiskRow, string> selector, string[] order) =>
        order.Select(name => new AccusedRiskGroupDto(name, rows.Count(r => selector(r) == name), Percent(rows.Count(r => selector(r) == name), rows.Count))).ToList();

    private static List<AccusedRiskGroupDto> DynamicRiskGroups(List<RiskRow> rows, Func<RiskRow, string> selector) =>
        rows.GroupBy(selector).OrderByDescending(g => g.Count()).ThenBy(g => g.Key)
            .Select(g => new AccusedRiskGroupDto(g.Key, g.Count(), Percent(g.Count(), rows.Count))).ToList();

    private static List<AccusedBreakdownDto> Breakdown(List<RiskRow> rows, Func<RiskRow, string?> selector) =>
        rows.GroupBy(r => string.IsNullOrWhiteSpace(selector(r)) ? "Unknown" : selector(r)!)
            .OrderByDescending(g => g.Count()).ThenBy(g => g.Key)
            .Select(g => new AccusedBreakdownDto(g.Key, g.Count(), g.Count(r => r.RedZone), Percent(g.Count(r => r.RedZone), g.Count()),
                g.Count(r => r.AcademicRisk is "High" or "Severe"),
                g.Count(r => r.FinancialRisk is "High" or "Severe"),
                g.Count(r => r.DropoutRisk is "High" or "Very High"),
                g.Count(r => r.Mms == "Poor–Poor"),
                Average(g.Select(r => r.Cgpa)), Average(g.Select(r => r.Dues))))
            .ToList();

    private static List<AccusedBreakdownDto> OrderedBreakdown(List<RiskRow> rows, Func<RiskRow, string?> selector, string[] order)
    {
        var breakdown = Breakdown(rows, selector).ToDictionary(x => x.Name);
        return order.Where(breakdown.ContainsKey).Select(name => breakdown[name]).ToList();
    }

    private static List<AccusedCrossTabDto> CrossTab(List<RiskRow> rows, Func<RiskRow, string> rowSelector, Func<RiskRow, string> columnSelector) =>
        rows.GroupBy(rowSelector).OrderByDescending(g => g.Count()).ThenBy(g => g.Key)
            .Select(g => new AccusedCrossTabDto(g.Key, g.GroupBy(columnSelector).OrderByDescending(x => x.Count())
                .Select(x => new AccusedCrossTabValueDto(x.Key, x.Count())).ToList())).ToList();

    private static string? TopName(List<RiskRow> rows, Func<RiskRow, string?> selector) => rows
        .Select(selector).Where(x => !string.IsNullOrWhiteSpace(x)).GroupBy(x => x!)
        .OrderByDescending(g => g.Count()).ThenBy(g => g.Key).Select(g => g.Key).FirstOrDefault();

    private static string AcademicRisk(decimal? cgpa) => !cgpa.HasValue ? "Unknown"
        : cgpa >= 3.50m ? "Low" : cgpa >= 3.00m ? "Moderate" : cgpa >= 2.50m ? "High" : "Severe";

    private static string CgpaBucket(RiskRow row) => !row.Cgpa.HasValue ? "Unknown"
        : row.Cgpa < 2.50m ? "Below 2.50"
        : row.Cgpa < 2.90m ? "2.50–2.89"
        : row.Cgpa < 3.00m ? "2.90–2.99"
        : row.Cgpa < 3.50m ? "3.00–3.49"
        : "3.50–4.00";

    private static string FinancialRisk(Student? student)
    {
        if (student?.RegistrationStatus == RegistrationStatus.Unregistered
            && (student.UnregistrationReason?.Contains("due", StringComparison.OrdinalIgnoreCase) == true
                || student.UnregistrationReason?.Contains("financial", StringComparison.OrdinalIgnoreCase) == true)) return "Severe";
        if (!student?.OutstandingDues.HasValue ?? true) return "Unknown";
        return student!.OutstandingDues > FinancialBoundary ? "High" : student.OutstandingDues > 0 ? "Moderate" : "Low";
    }

    private static string CcrRisk(decimal? completed, decimal? expected)
    {
        if (!completed.HasValue || !expected.HasValue || expected <= 0) return "Unknown";
        var ratio = completed.Value / expected.Value;
        return ratio >= .85m ? "Low" : ratio >= .70m ? "Moderate" : ratio >= .50m ? "High" : "Severe";
    }

    private static string Mms(decimal? cgpa, decimal? dues)
    {
        if (!cgpa.HasValue || !dues.HasValue) return "Unknown";
        var academicFit = cgpa >= MmsCgpaBoundary;
        var financialFit = dues <= FinancialBoundary;
        return (academicFit, financialFit) switch
        {
            (true, true) => "Fit–Fit",
            (false, true) => "Poor–Fit",
            (true, false) => "Fit–Poor",
            _ => "Poor–Poor"
        };
    }

    private static int? DropoutScore(string academic, string financial, string ccr, string mms, RegistrationStatus? registration, RiskSeverity? complaintSeverity)
    {
        if (academic == "Unknown" || financial == "Unknown" || ccr == "Unknown" || !registration.HasValue) return null;
        var score = 0;
        if (academic is "High" or "Severe") score += 2;
        if (financial is "High" or "Severe") score += 2;
        if (registration == RegistrationStatus.Unregistered) score += 3;
        if (complaintSeverity == RiskSeverity.High) score += 1;
        if (mms == "Poor–Poor") score += 1;
        if (ccr is "High" or "Severe") score += 1;
        return score;
    }

    private static string DropoutRisk(int? score) => !score.HasValue ? "Unknown"
        : score >= 7 ? "Very High" : score >= 5 ? "High" : score >= 3 ? "Moderate" : "Low";

    private static bool IsRedZone(string academic, string financial, string ccr, string dropout, RegistrationStatus? registration)
    {
        if (academic == "Severe" || dropout is "High" or "Very High") return true;
        if (registration == RegistrationStatus.Unregistered && academic is not ("Low" or "Unknown")) return true;
        var highDimensions = new[] { academic, financial, ccr }.Count(value => value is "High" or "Severe");
        return highDimensions >= 2;
    }

    private IQueryable<Case> VisibleCases(string role, Guid? userId)
    {
        role = role.NormalizeRoleKey();
        var source = _db.Cases.AsQueryable();
        if (role == "student") return source.Where(c => userId.HasValue && c.SubmittedByUserId == userId.Value);
        if (role == "female-coordinator") return source.Where(c => c.IsConfidential || c.Type == CaseType.Confidential || c.Type == CaseType.Type1 || c.SubmitterGender == Gender.Female);
        if (role is "proctor" or "coordinator" or "super-admin" or "vc") return source;
        return source.Where(c => (c.ForwardedToRole == role || (userId.HasValue &&
            (c.AssignedToId == userId.Value || c.Assignments.Any(a => a.UserId == userId.Value))))
            && ((!c.IsConfidential && c.Type != CaseType.Confidential) || role == "sexual-harassment-committee"));
    }

    private static bool HasLegacyAccused(Case c) => !string.IsNullOrWhiteSpace(c.AccusedName)
        || !string.IsNullOrWhiteSpace(c.AccusedId) || !string.IsNullOrWhiteSpace(c.AccusedDepartment);
    private static string NormalizeId(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();
    private static decimal Percent(int value, int total) => total == 0 ? 0 : Math.Round(value * 100m / total, 1);
    private static decimal? Average(IEnumerable<decimal?> values)
    {
        var present = values.Where(v => v.HasValue).Select(v => v!.Value).ToList();
        return present.Count == 0 ? null : Math.Round(present.Average(), 2);
    }
    private static decimal? Median(IEnumerable<decimal?> values)
    {
        var present = values.Where(v => v.HasValue).Select(v => v!.Value).OrderBy(v => v).ToList();
        if (present.Count == 0) return null;
        var middle = present.Count / 2;
        return Math.Round(present.Count % 2 == 1 ? present[middle] : (present[middle - 1] + present[middle]) / 2m, 2);
    }

    private static decimal? CgpaGap(List<RiskRow> rows)
    {
        var redZone = Average(rows.Where(r => r.RedZone).Select(r => r.Cgpa));
        var nonRedZone = Average(rows.Where(r => !r.RedZone).Select(r => r.Cgpa));
        return redZone.HasValue && nonRedZone.HasValue ? Math.Round(redZone.Value - nonRedZone.Value, 2) : null;
    }

    private sealed class RiskRow
    {
        public DateTime Date { get; init; }
        public string StudentId { get; init; } = string.Empty;
        public bool StudentMatched { get; init; }
        public string Complaint { get; init; } = string.Empty;
        public string? Department { get; init; }
        public string? Batch { get; init; }
        public string? Gender { get; init; }
        public decimal? Cgpa { get; init; }
        public decimal? Dues { get; init; }
        public decimal? CompletedCredits { get; init; }
        public decimal? ExpectedCredits { get; init; }
        public string AcademicRisk { get; init; } = "Unknown";
        public string FinancialRisk { get; init; } = "Unknown";
        public string CcrRisk { get; init; } = "Unknown";
        public string Mms { get; init; } = "Unknown";
        public int? RiskScore { get; init; }
        public string DropoutRisk { get; init; } = "Unknown";
        public bool RedZone { get; init; }
    }
}
