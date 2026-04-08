using System.Text;
using System.Text.RegularExpressions;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Application.DTOs.Cases;
using PROCTOR.Application.DTOs.Dashboard;
using PROCTOR.Application.DTOs.Documents;
using PROCTOR.Application.DTOs.Hearings;
using PROCTOR.Application.DTOs.Notes;
using PROCTOR.Application.DTOs.Permissions;
using PROCTOR.Application.DTOs.Reports;
using PROCTOR.Application.DTOs.Users;
using PROCTOR.Application.DTOs.Articles;
using PROCTOR.Application.DTOs.Ranks;

namespace PROCTOR.Application.Mapping;

public static class MappingExtensions
{
    public static string ToKebabCase(this Enum value)
    {
        var name = value.ToString();
        if (string.IsNullOrEmpty(name)) return name;

        var sb = new StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0)
            {
                var prevIsLower = char.IsLower(name[i - 1]);
                var prevIsDigit = char.IsDigit(name[i - 1]);
                if (prevIsLower || prevIsDigit)
                {
                    sb.Append('-');
                }
            }
            else if (char.IsDigit(c) && i > 0 && char.IsLetter(name[i - 1]))
            {
                sb.Append('-');
            }
            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }

    public static T ParseEnum<T>(string kebabCase) where T : struct, Enum
    {
        // Convert kebab-case to PascalCase
        var parts = kebabCase.Split('-');
        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            if (part.Length > 0)
            {
                sb.Append(char.ToUpperInvariant(part[0]));
                if (part.Length > 1)
                    sb.Append(part[1..]);
            }
        }

        var pascalCase = sb.ToString();
        if (Enum.TryParse<T>(pascalCase, ignoreCase: true, out var result))
            return result;

        throw new ArgumentException($"Invalid value '{kebabCase}' for enum {typeof(T).Name}");
    }

    public static UserDto ToDto(this User user) => new()
    {
        Id = user.Id.ToString(),
        Name = user.Name,
        Email = user.Email,
        Role = user.Role.ToKebabCase(),
        Avatar = user.Avatar,
        Rank = user.RankName
    };

    public static CaseDto ToDto(this Case c) => new()
    {
        Id = c.Id.ToString(),
        CaseNumber = c.CaseNumber,
        StudentName = c.StudentName,
        StudentId = c.StudentId,
        Type = c.Type.ToKebabCase(),
        Status = c.Status.ToKebabCase(),
        Priority = c.Priority.ToKebabCase(),
        AssignedTo = c.AssignedTo?.Name,
        CreatedDate = c.CreatedAt.ToString("o"),
        UpdatedDate = c.UpdatedAt.ToString("o"),
        Description = c.Description,
        Verdict = c.Verdict,
        Recommendation = c.Recommendation,
        ForwardedToRole = c.ForwardedToRole,
        StudentDepartment = c.StudentDepartment,
        StudentContact = c.StudentContact,
        StudentAdvisorName = c.StudentAdvisorName,
        StudentFatherName = c.StudentFatherName,
        StudentFatherContact = c.StudentFatherContact,
        AccusedName = c.AccusedName,
        AccusedId = c.AccusedId,
        AccusedDepartment = c.AccusedDepartment,
        AccusedContact = c.AccusedContact,
        AccusedGuardianContact = c.AccusedGuardianContact,
        VideoLink = c.VideoLink,
        IncidentDate = c.IncidentDate?.ToString("o"),
        Complainants = c.Complainants.OrderBy(x => x.Order).Select(x => x.ToDto()).ToList(),
        AccusedPersons = c.AccusedPersons.OrderBy(x => x.Order).Select(x => x.ToDto()).ToList(),
        Documents = c.Documents.Select(d => d.ToDto()).ToList(),
        Notes = c.Notes.Select(n => n.ToDto()).ToList(),
        Hearings = c.Hearings.Select(h => h.ToDto()).ToList(),
        Timeline = c.TimelineEvents.Select(t => t.ToDto()).ToList(),
        Reports = c.Reports.Select(r => r.ToDto()).ToList()
    };

    public static CaseListDto ToListDto(this Case c) => new()
    {
        Id = c.Id.ToString(),
        CaseNumber = c.CaseNumber,
        StudentName = c.StudentName,
        StudentId = c.StudentId,
        Type = c.Type.ToKebabCase(),
        Status = c.Status.ToKebabCase(),
        Priority = c.Priority.ToKebabCase(),
        AssignedTo = c.AssignedTo?.Name,
        CreatedDate = c.CreatedAt.ToString("o"),
        UpdatedDate = c.UpdatedAt.ToString("o"),
        Description = c.Description,
        ForwardedToRole = c.ForwardedToRole
    };

    public static DocumentDto ToDto(this Document d) => new()
    {
        Id = d.Id.ToString(),
        Name = d.Name,
        Type = d.Type.ToKebabCase(),
        Url = d.Url,
        UploadedBy = d.UploadedBy,
        UploadedByRole = d.UploadedByRole,
        UploadedDate = d.CreatedAt.ToString("o")
    };

    public static NoteDto ToDto(this Note n) => new()
    {
        Id = n.Id.ToString(),
        Content = n.Content,
        Author = n.Author,
        CreatedDate = n.CreatedAt.ToString("o")
    };

    public static HearingDto ToDto(this Hearing h) => new()
    {
        Id = h.Id.ToString(),
        CaseId = h.CaseId.ToString(),
        Date = h.Date,
        Time = h.Time,
        Location = h.Location,
        Participants = h.Participants,
        Status = h.Status.ToKebabCase(),
        Notes = h.Notes,
        Remarks = h.Remarks
    };

    public static RecentActivityDto ToDto(this TimelineEvent t) => new()
    {
        Id = t.Id.ToString(),
        Action = t.Action,
        Description = t.Description,
        User = t.User,
        Timestamp = t.CreatedAt.ToString("o")
    };

    public static RoleDto ToDto(this Role r) => new()
    {
        Id = r.Id.ToString(),
        RoleName = r.RoleName.ToKebabCase(),
        DisplayName = r.DisplayName,
        MenuPermissions = r.MenuPermissions.Select(mp => mp.ToDto()).ToList()
    };

    public static ReportDto ToDto(this Report r) => new()
    {
        Id = r.Id.ToString(),
        CaseId = r.CaseId.ToString(),
        Content = r.Content,
        CreatedByName = r.CreatedByName,
        IsDraft = r.IsDraft,
        IsFinal = r.IsFinal,
        CreatedDate = r.CreatedAt.ToString("o"),
        SectionsJson = r.SectionsJson
    };

    public static MenuPermissionDto ToDto(this MenuPermission mp) => new()
    {
        Id = mp.Id.ToString(),
        MenuKey = mp.MenuKey,
        CanCreate = mp.CanCreate,
        CanRead = mp.CanRead,
        CanUpdate = mp.CanUpdate,
        CanDelete = mp.CanDelete
    };

    public static CaseComplainantDto ToDto(this CaseComplainant c) => new()
    {
        Id = c.Id.ToString(), Name = c.Name, StudentId = c.StudentId,
        Department = c.Department, Contact = c.Contact, AdvisorName = c.AdvisorName,
        FatherName = c.FatherName, FatherContact = c.FatherContact
    };

    public static CaseAccusedDto ToDto(this CaseAccused a) => new()
    {
        Id = a.Id.ToString(), Name = a.Name, AccusedStudentId = a.AccusedStudentId,
        Department = a.Department, Contact = a.Contact, GuardianContact = a.GuardianContact
    };
}
