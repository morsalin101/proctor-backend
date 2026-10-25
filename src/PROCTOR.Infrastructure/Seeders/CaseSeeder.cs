using Microsoft.EntityFrameworkCore;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.Infrastructure.Seeders;

public static class CaseSeeder
{
    public static async Task SeedAsync(ProctorDbContext context)
    {
        await RepairSeededConfidentialCasesAsync(context);
        await CompleteSeededCaseWorkflowAsync(context);

        // The description contains a stable demo marker. The previous check used a word that
        // was not present in the seeded text, so every restart could insert another 20 cases.
        if (await context.Cases.AnyAsync(c => c.Description.Contains("ডামি কেস"))) return;

        var categories = await context.CaseCategories.Where(c => c.IsActive).ToListAsync();
        var studentUser = await context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Student);

        if (categories.Count == 0 || studentUser == null) return;

        var rand = new Random(42); // deterministic for reproducible seeds

        var firstNamesMale = new[] { "রহিম", "করিম", "তরিকুল", "তানভীর", "মোঃ আল আমিন", "আরাফাত", "হাসান", "কামরুল", "সাব্বির", "মেহেদী" };
        var firstNamesFemale = new[] { "সাদিয়া", "ফারহানা", "সামিহা", "নুসরাত", "তাসনিম", "ফারিহা", "জান্নাতুল", "সুমাইয়া" };
        var lastNames = new[] { "উদ্দিন", "হোসেন", "ইসলাম", "রহমান", "আহমেদ", "হক", "আক্তার", "জাহান", "চৌধুরী", "খান" };
        var departments = new[] { "সিএসই", "সফটওয়্যার ইঞ্জিনিয়ারিং", "বিবিএ", "ত্রিপল-ই", "টেক্সটাইল", "সিভিল", "ইংরেজি", "আইন" };

        var cases = new List<Case>();

        for (int i = 1; i <= 20; i++)
        {
            var type = i % 3 == 0 ? CaseType.Type1 : (i % 7 == 0 ? CaseType.Confidential : CaseType.Type2);
            // Confidentiality is a deliberate decision made by the female administrative
            // officer after female-track intake. Never create a male confidential demo case.
            var isMale = type != CaseType.Confidential && rand.Next(2) == 0;
            var firstName = isMale ? firstNamesMale[rand.Next(firstNamesMale.Length)] : firstNamesFemale[rand.Next(firstNamesFemale.Length)];
            var lastName = lastNames[rand.Next(lastNames.Length)];
            var studentName = $"{firstName} {lastName}";
            
            var accusedIsMale = rand.Next(2) == 0;
            var accusedFirstName = accusedIsMale ? firstNamesMale[rand.Next(firstNamesMale.Length)] : firstNamesFemale[rand.Next(firstNamesFemale.Length)];
            var accusedLastName = lastNames[rand.Next(lastNames.Length)];
            var accusedName = $"{accusedFirstName} {accusedLastName}";

            var dept = departments[rand.Next(departments.Length)];
            var category = categories[rand.Next(categories.Count)];
            var incidentDate = DateTime.UtcNow.AddDays(-rand.Next(1, 100));
            var term = incidentDate.Month switch
            {
                >= 1 and <= 4 => "Spring",
                >= 5 and <= 8 => "Summer",
                _ => "Fall"
            };

            var newCase = new Case
            {
                Id = Guid.NewGuid(),
                CaseNumber = $"DIU-{DateTime.UtcNow.Year}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                StudentName = studentName,
                StudentId = $"0{rand.Next(1, 9)}{rand.Next(1, 3)}-{rand.Next(11, 40)}-{rand.Next(1000, 9999)}",
                Type = type == CaseType.Confidential ? CaseType.Type2 : type,
                IsConfidential = type == CaseType.Confidential,
                Status = type == CaseType.Confidential ? CaseStatus.Assigned : CaseStatus.Submitted,
                Priority = type == CaseType.Type1 ? Priority.High : Priority.Medium,
                Description = $"এটি {dept} বিভাগে {studentName} এর সাথে ঘটে যাওয়া একটি ঘটনার ডামি কেস। ঘটনাটি সম্পর্কে বিস্তারিত তদন্তের জন্য অভিযোগটি দাখিল করা হলো।",
                SubmittedByUserId = studentUser.Id,
                SubmitterGender = isMale ? Gender.Male : Gender.Female,
                CategoryId = category.Id,
                IncidentDate = incidentDate,
                AcademicSemester = $"{term}-{incidentDate.Year}",
                StudentDepartment = dept,
                StudentSemester = rand.Next(1, 13),
                StudentCgpa = (decimal)(2.5 + rand.NextDouble() * 1.5),
                StudentContact = $"017{rand.Next(10000000, 99999999)}",
                AccusedName = accusedName,
                AccusedId = $"0{rand.Next(1, 9)}{rand.Next(1, 3)}-{rand.Next(11, 40)}-{rand.Next(1000, 9999)}",
                AccusedDepartment = departments[rand.Next(departments.Length)],
                AccusedContact = $"017{rand.Next(10000000, 99999999)}",
                ForwardedToRole = type == CaseType.Confidential ? "sexual-harassment-committee" : null,
                CreatedAt = incidentDate.AddDays(1)
            };

            newCase.TimelineEvents.Add(new TimelineEvent
            {
                Id = Guid.NewGuid(), CaseId = newCase.Id, Action = "Case Created",
                Description = $"Case {newCase.CaseNumber} was created.", User = studentName,
                CreatedAt = newCase.CreatedAt
            });

            if (type == CaseType.Confidential)
            {
                newCase.TimelineEvents.Add(new TimelineEvent
                {
                    Id = Guid.NewGuid(), CaseId = newCase.Id, Action = "Marked Confidential",
                    Description = "Female-submitted case explicitly marked confidential by the female Assistant Administrative Officer before forwarding.",
                    User = "Female Assistant Administrative Officer", CreatedAt = newCase.CreatedAt.AddHours(1)
                });
                newCase.TimelineEvents.Add(new TimelineEvent
                {
                    Id = Guid.NewGuid(), CaseId = newCase.Id, Action = "Case Forwarded",
                    Description = "Confidential case forwarded to the Sexual Harassment Committee.",
                    User = "Female Assistant Administrative Officer", CreatedAt = newCase.CreatedAt.AddHours(1)
                });
            }

            cases.Add(newCase);
        }

        await context.Cases.AddRangeAsync(cases);
        await context.SaveChangesAsync();
        await CompleteSeededCaseWorkflowAsync(context);
    }

    /// <summary>
    /// Keeps the Bangla demo cases useful on both new and existing databases. Every demo case
    /// references a real seeded student (so batch/CGPA analytics work), has real seeded staff
    /// assignments, and contains a coherent workflow up to the report-creation handoff.
    /// Type-1 incidents use their separate acknowledge/close flow and never receive reports.
    /// Existing Type-3 or reported cases are not moved backwards.
    /// </summary>
    private static async Task CompleteSeededCaseWorkflowAsync(ProctorDbContext context)
    {
        var seededCases = await context.Cases
            .Where(c => c.Description.Contains("ডামি কেস"))
            .Include(c => c.Assignments)
            .Include(c => c.TimelineEvents)
            .Include(c => c.Notes)
            .Include(c => c.Hearings)
            .Include(c => c.Verifications)
            .Include(c => c.Reports)
            .Include(c => c.Type3Workflow)
            .Include(c => c.AccusedPersons)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
        if (seededCases.Count == 0) return;

        var users = await context.Users.Where(user => user.IsActive).ToListAsync();
        var studentUsersByEmail = users
            .Where(user => user.Role == UserRole.Student)
            .ToDictionary(user => user.Email.ToLowerInvariant());
        var students = await context.Students
            .Where(student => student.IsActive && student.Email != null)
            .OrderBy(student => student.StudentId)
            .ToListAsync();
        var linkedStudents = students
            .Where(student => student.Email != null && studentUsersByEmail.ContainsKey(student.Email.ToLowerInvariant()))
            .ToList();
        if (linkedStudents.Count == 0) return;

        var proctors = users.Where(user => user.Role == UserRole.Proctor).OrderBy(user => user.Name).ToList();
        var coordinators = users.Where(user => user.Role == UserRole.Coordinator).OrderBy(user => user.Name).ToList();
        var femaleCoordinators = users.Where(user => user.Role == UserRole.FemaleCoordinator).OrderBy(user => user.Name).ToList();
        var fallbackHandlers = users.Where(user => user.Role is UserRole.Proctor or UserRole.Coordinator
            or UserRole.FemaleCoordinator or UserRole.AssistantProctor or UserRole.DeputyProctor).ToList();
        if (fallbackHandlers.Count == 0) return;

        var banglaDescriptions = new[]
        {
            "পরীক্ষার হলে অসদুপায় অবলম্বনের অভিযোগ পাওয়ার পর প্রাথমিক তথ্য যাচাই করা হয়েছে। এটি একটি ডামি কেস।",
            "বিশ্ববিদ্যালয় ক্যাম্পাসে শৃঙ্খলাভঙ্গের ঘটনায় সংশ্লিষ্ট শিক্ষার্থীদের বক্তব্য গ্রহণ করা হয়েছে। এটি একটি ডামি কেস।",
            "সহপাঠীর সঙ্গে অসদাচরণের অভিযোগের প্রমাণ ও সাক্ষীদের বক্তব্য পর্যালোচনা করা হয়েছে। এটি একটি ডামি কেস।",
            "বিশ্ববিদ্যালয়ের সম্পদ ক্ষতিগ্রস্ত করার অভিযোগে প্রাথমিক তদন্ত সম্পন্ন হয়েছে। এটি একটি ডামি কেস।",
            "অনলাইন মাধ্যমে হয়রানির অভিযোগে প্রাসঙ্গিক তথ্য ও নথি সংগ্রহ করা হয়েছে। এটি একটি ডামি কেস।"
        };
        var banglaRecommendations = new[]
        {
            "সংগৃহীত তথ্য প্রতিবেদন আকারে উপস্থাপন করে পরবর্তী সিদ্ধান্ত গ্রহণের সুপারিশ করা হলো।",
            "শুনানির বক্তব্য ও প্রমাণ প্রতিবেদনে অন্তর্ভুক্ত করে কর্তৃপক্ষের বিবেচনার জন্য পাঠানোর সুপারিশ করা হলো।",
            "বিশ্ববিদ্যালয়ের শৃঙ্খলা বিধি অনুযায়ী তদন্ত প্রতিবেদন প্রস্তুত করার সুপারিশ করা হলো।"
        };

        for (var index = 0; index < seededCases.Count; index++)
        {
            var caseItem = seededCases[index];
            var student = linkedStudents[index % linkedStudents.Count];
            var studentUser = studentUsersByEmail[student.Email!.ToLowerInvariant()];

            caseItem.StudentId = student.StudentId;
            caseItem.StudentName = student.Name;
            caseItem.StudentDepartment = student.Department;
            caseItem.StudentCgpa = student.Cgpa;
            caseItem.StudentContact = student.Contact;
            caseItem.StudentAdvisorName = student.AdvisorName;
            caseItem.StudentFatherName = student.FatherName;
            caseItem.StudentFatherContact = student.FatherContact;
            caseItem.SubmitterGender = student.Gender;
            caseItem.SubmittedByUserId = studentUser.Id;
            caseItem.StudentSemester ??= (index % 12) + 1;
            caseItem.Description = banglaDescriptions[index % banglaDescriptions.Length];
            caseItem.Recommendation ??= banglaRecommendations[index % banglaRecommendations.Length];
            caseItem.IncidentDate = new DateTime(2022 + index % 5, 1 + index % 12, 1 + index % 25, 0, 0, 0, DateTimeKind.Utc);

            var accusedStudent = students[(index + 3) % students.Count];
            var accused = caseItem.AccusedPersons.OrderBy(person => person.Order).FirstOrDefault();
            if (accused is null)
            {
                accused = new CaseAccused
                {
                    Id = Guid.NewGuid(), CaseId = caseItem.Id, Order = 0,
                    CreatedAt = caseItem.CreatedAt, UpdatedAt = caseItem.UpdatedAt
                };
                caseItem.AccusedPersons.Add(accused);
                context.CaseAccusedPersons.Add(accused);
            }
            accused.Name = accusedStudent.Name;
            accused.AccusedStudentId = accusedStudent.StudentId;
            accused.Department = accusedStudent.Department;
            accused.Contact = accusedStudent.Contact;
            accused.GuardianContact = accusedStudent.GuardianContact;
            accused.Cgpa = accusedStudent.Cgpa;
            caseItem.AccusedName = accused.Name;
            caseItem.AccusedId = accused.AccusedStudentId;
            caseItem.AccusedDepartment = accused.Department;
            caseItem.AccusedContact = accused.Contact;
            caseItem.AccusedGuardianContact = accused.GuardianContact;

            var preferredHandlers = caseItem.IsConfidential || student.Gender == Gender.Female
                ? femaleCoordinators
                : coordinators;
            if (preferredHandlers.Count == 0) preferredHandlers = proctors.Count > 0 ? proctors : fallbackHandlers;
            var primary = preferredHandlers[index % preferredHandlers.Count];
            var secondary = proctors.FirstOrDefault(user => user.Id != primary.Id);

            if (!caseItem.Assignments.Any(assignment => assignment.IsActive))
            {
                var primaryAssignment = new CaseAssignment
                {
                    Id = Guid.NewGuid(),
                    CaseId = caseItem.Id,
                    UserId = primary.Id,
                    AssignedById = proctors.FirstOrDefault()?.Id,
                    AssignedAt = caseItem.CreatedAt.AddHours(3),
                    IsActive = true,
                    IsPrimary = true,
                    CreatedAt = caseItem.CreatedAt.AddHours(3),
                    UpdatedAt = caseItem.CreatedAt.AddHours(3)
                };
                caseItem.Assignments.Add(primaryAssignment);
                context.CaseAssignments.Add(primaryAssignment);
                if (secondary is not null)
                {
                    var secondaryAssignment = new CaseAssignment
                    {
                        Id = Guid.NewGuid(),
                        CaseId = caseItem.Id,
                        UserId = secondary.Id,
                        AssignedById = proctors.FirstOrDefault()?.Id,
                        AssignedAt = caseItem.CreatedAt.AddHours(4),
                        IsActive = true,
                        IsPrimary = false,
                        CreatedAt = caseItem.CreatedAt.AddHours(4),
                        UpdatedAt = caseItem.CreatedAt.AddHours(4)
                    };
                    caseItem.Assignments.Add(secondaryAssignment);
                    context.CaseAssignments.Add(secondaryAssignment);
                }
            }

            caseItem.AssignedToId ??= caseItem.Assignments
                .Where(assignment => assignment.IsActive)
                .OrderByDescending(assignment => assignment.IsPrimary)
                .Select(assignment => (Guid?)assignment.UserId)
                .FirstOrDefault() ?? primary.Id;
            caseItem.ForwardedToRole ??= RoleKey(primary.Role);

            if (caseItem.Type == CaseType.Type1)
            {
                caseItem.IsAcknowledged = true;
                caseItem.AcknowledgedAt ??= caseItem.CreatedAt.AddMinutes(20);
                caseItem.AcknowledgedById ??= primary.Id;
                caseItem.AcknowledgedByName ??= primary.Name;
                caseItem.AcknowledgmentComment ??= "ঘটনাটি গ্রহণ করা হয়েছে এবং তাৎক্ষণিকভাবে দায়িত্বপ্রাপ্ত কর্মকর্তাকে জানানো হয়েছে।";
                caseItem.Status = CaseStatus.Closed;
                caseItem.ClosingMessage ??= "তাৎক্ষণিক ব্যবস্থা ও সংশ্লিষ্ট পক্ষের বক্তব্য গ্রহণের পর ঘটনাটি নিষ্পত্তি করা হয়েছে।";
                caseItem.ClosedAt ??= caseItem.CreatedAt.AddHours(5);
                caseItem.ClosedByName ??= primary.Name;
                AddTimeline(context, caseItem, "Incident Acknowledged", "দায়িত্বপ্রাপ্ত কর্মকর্তা ঘটনাটি গ্রহণ করেছেন।", primary.Name, caseItem.CreatedAt.AddMinutes(20));
                AddTimeline(context, caseItem, "Case Closed", "তাৎক্ষণিক ব্যবস্থা শেষে টাইপ-১ ঘটনাটি বন্ধ করা হয়েছে।", primary.Name, caseItem.CreatedAt.AddHours(5));
                caseItem.UpdatedAt = caseItem.ClosedAt.Value;
                continue;
            }

            // Do not rewind a case that has already entered report/Type-3 processing.
            if (caseItem.Type == CaseType.Type3 || caseItem.Type3Workflow is not null || caseItem.Reports.Any())
                continue;

            caseItem.Status = CaseStatus.HearingCompleted;
            caseItem.Verdict ??= "শুনানিতে উভয় পক্ষের বক্তব্য ও উপস্থাপিত প্রমাণ নথিভুক্ত করা হয়েছে।";

            if (!caseItem.Verifications.Any())
            {
                var verification = new CaseVerification
                {
                    Id = Guid.NewGuid(), CaseId = caseItem.Id,
                    Comment = "অভিযোগের পরিচয়, যোগাযোগের তথ্য ও প্রাথমিক প্রমাণ যাচাই করা হয়েছে।",
                    ChecklistResultsJson = "[]", CreatedByName = primary.Name,
                    CreatedAt = caseItem.CreatedAt.AddDays(1), UpdatedAt = caseItem.CreatedAt.AddDays(1)
                };
                caseItem.Verifications.Add(verification);
                context.CaseVerifications.Add(verification);
            }
            if (!caseItem.Hearings.Any())
            {
                var hearingAt = caseItem.CreatedAt.AddDays(4);
                var hearing = new Hearing
                {
                    Id = Guid.NewGuid(), CaseId = caseItem.Id,
                    Date = hearingAt.ToString("yyyy-MM-dd"), Time = "10:30",
                    Location = "প্রক্টর অফিস সভাকক্ষ",
                    Participants = new List<string> { student.Name, caseItem.AccusedName ?? "অভিযুক্ত শিক্ষার্থী", primary.Name },
                    Status = HearingStatus.Completed,
                    Notes = "অভিযোগকারী ও অভিযুক্ত উভয় পক্ষকে শুনানির সুযোগ দেওয়া হয়েছে।",
                    Remarks = "উভয় পক্ষের বক্তব্য গ্রহণ এবং প্রাসঙ্গিক নথি পর্যালোচনা সম্পন্ন হয়েছে।",
                    CreatedById = primary.Id, CreatedByName = primary.Name,
                    ConductedById = primary.Id, ConductedByName = primary.Name, ConductedAt = hearingAt,
                    CreatedAt = caseItem.CreatedAt.AddDays(2), UpdatedAt = hearingAt
                };
                caseItem.Hearings.Add(hearing);
                context.Hearings.Add(hearing);
            }
            if (!caseItem.Notes.Any(note => note.Author == primary.Name))
            {
                var note = new Note
                {
                    Id = Guid.NewGuid(), CaseId = caseItem.Id,
                    Content = "তদন্ত ও শুনানির প্রয়োজনীয় তথ্য সংগ্রহ সম্পন্ন। এখন প্রতিবেদন প্রস্তুত করা যাবে।",
                    Author = primary.Name, CreatedAt = caseItem.CreatedAt.AddDays(4), UpdatedAt = caseItem.CreatedAt.AddDays(4)
                };
                caseItem.Notes.Add(note);
                context.Notes.Add(note);
            }

            AddTimeline(context, caseItem, "Case Assigned", $"কেসটি {primary.Name}-কে দায়িত্ব দেওয়া হয়েছে।", primary.Name, caseItem.CreatedAt.AddHours(3));
            AddTimeline(context, caseItem, "Case Verified", "অভিযোগের প্রাথমিক তথ্য ও নথি যাচাই সম্পন্ন হয়েছে।", primary.Name, caseItem.CreatedAt.AddDays(1));
            AddTimeline(context, caseItem, "Hearing Scheduled", "সংশ্লিষ্ট পক্ষদের উপস্থিতিতে শুনানির সময় নির্ধারণ করা হয়েছে।", primary.Name, caseItem.CreatedAt.AddDays(2));
            AddTimeline(context, caseItem, "Hearing Completed", "উভয় পক্ষের বক্তব্য ও প্রমাণ গ্রহণ করে শুনানি সম্পন্ন হয়েছে।", primary.Name, caseItem.CreatedAt.AddDays(4));
            AddTimeline(context, caseItem, "Ready for Report", "কেসটির তদন্ত ধাপ সম্পন্ন; এখন প্রতিবেদন তৈরি করা যাবে।", primary.Name, caseItem.CreatedAt.AddDays(4).AddHours(1));
            caseItem.UpdatedAt = caseItem.CreatedAt.AddDays(4).AddHours(1);
        }

        await context.SaveChangesAsync();
    }

    private static void AddTimeline(ProctorDbContext context, Case caseItem, string action, string description, string user, DateTime timestamp)
    {
        if (caseItem.TimelineEvents.Any(item => item.Action == action)) return;
        var timelineEvent = new TimelineEvent
        {
            Id = Guid.NewGuid(), CaseId = caseItem.Id, Action = action,
            Description = description, User = user,
            CreatedAt = timestamp, UpdatedAt = timestamp
        };
        caseItem.TimelineEvents.Add(timelineEvent);
        context.TimelineEvents.Add(timelineEvent);
    }

    private static string RoleKey(UserRole role) => role switch
    {
        UserRole.FemaleCoordinator => "female-coordinator",
        UserRole.AssistantProctor => "assistant-proctor",
        UserRole.DeputyProctor => "deputy-proctor",
        _ => role.ToString().ToLowerInvariant()
    };

    private static async Task RepairSeededConfidentialCasesAsync(ProctorDbContext context)
    {
        var seeded = await context.Cases
            .Where(c => c.Type == CaseType.Confidential && c.Description.Contains("ডামি কেস"))
            .ToListAsync();
        if (seeded.Count == 0) return;

        var ids = seeded.Select(c => c.Id).ToList();
        var existingActions = await context.TimelineEvents
            .Where(e => ids.Contains(e.CaseId))
            .Select(e => new { e.CaseId, e.Action })
            .ToListAsync();

        foreach (var c in seeded)
        {
            if (c.SubmitterGender != Gender.Female)
            {
                var previousName = c.StudentName;
                c.StudentName = "সামিহা আক্তার";
                c.Description = c.Description.Replace(previousName, c.StudentName, StringComparison.Ordinal);
            }
            c.SubmitterGender = Gender.Female;
            c.Status = CaseStatus.Assigned;
            c.ForwardedToRole = "sexual-harassment-committee";

            if (!existingActions.Any(e => e.CaseId == c.Id && e.Action == "Marked Confidential"))
            {
                context.TimelineEvents.Add(new TimelineEvent
                {
                    Id = Guid.NewGuid(), CaseId = c.Id, Action = "Marked Confidential",
                    Description = "Female-submitted case explicitly marked confidential by the female Assistant Administrative Officer before forwarding.",
                    User = "Female Assistant Administrative Officer", CreatedAt = c.CreatedAt.AddHours(1)
                });
            }

            if (!existingActions.Any(e => e.CaseId == c.Id && e.Action == "Case Forwarded"))
            {
                context.TimelineEvents.Add(new TimelineEvent
                {
                    Id = Guid.NewGuid(), CaseId = c.Id, Action = "Case Forwarded",
                    Description = "Confidential case forwarded to the Sexual Harassment Committee.",
                    User = "Female Assistant Administrative Officer", CreatedAt = c.CreatedAt.AddHours(1)
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
