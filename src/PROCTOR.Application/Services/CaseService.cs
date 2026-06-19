using System.Linq.Expressions;
using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Cases;
using PROCTOR.Application.DTOs.Reports;
using PROCTOR.Application.Interfaces;
using PROCTOR.Application.Mapping;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Domain.Interfaces;

namespace PROCTOR.Application.Services;

public class CaseService : ICaseService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IWorkflowService _workflowService;
    private readonly ISystemSettingService _systemSettingService;
    private readonly IEmailService _emailService;
    private readonly IRepository<CaseCategory> _categoryRepo;

    public CaseService(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        IWorkflowService workflowService,
        ISystemSettingService systemSettingService,
        IEmailService emailService,
        IRepository<CaseCategory> categoryRepo)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _workflowService = workflowService;
        _systemSettingService = systemSettingService;
        _emailService = emailService;
        _categoryRepo = categoryRepo;
    }

    public async Task<ApiResponse<PagedResult<CaseListDto>>> GetCasesAsync(
        string? status, string? type, string? priority, string? search, int page, int pageSize, string? userRole = null)
    {
        CaseStatus? statusEnum = status is not null ? MappingExtensions.ParseEnum<CaseStatus>(status) : null;
        CaseType? typeEnum = type is not null ? MappingExtensions.ParseEnum<CaseType>(type) : null;
        Priority? priorityEnum = priority is not null ? MappingExtensions.ParseEnum<Priority>(priority) : null;

        var cases = await _unitOfWork.Cases.GetFilteredAsync(statusEnum, typeEnum, priorityEnum, search, page, pageSize, userRole);
        var totalCount = await _unitOfWork.Cases.GetFilteredCountAsync(statusEnum, typeEnum, priorityEnum, search, userRole);

        var result = new PagedResult<CaseListDto>
        {
            Items = cases.Select(c => c.ToListDto()),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return ApiResponse<PagedResult<CaseListDto>>.SuccessResponse(result);
    }

    // A case is on the "female track" (handled only by the Female Coordinator) when the
    // complainant is female or the case is confidential.
    public static bool IsFemaleTrack(Case c) => c.Type == CaseType.Confidential || c.SubmitterGender == Gender.Female;

    // The two coordinator roles are gender-separated; every other role is unaffected.
    private static bool CoordinatorMayView(string? role, Case c)
    {
        if (role == "coordinator") return !IsFemaleTrack(c);        // male coordinator: never female-track
        if (role == "female-coordinator") return IsFemaleTrack(c);  // female coordinator: only female-track
        return true;
    }

    public async Task<ApiResponse<CaseDto>> GetCaseByIdAsync(Guid id, string? userRole = null)
    {
        var c = await _unitOfWork.Cases.GetByIdWithDetailsAsync(id);
        if (c is null)
            return ApiResponse<CaseDto>.FailResponse("Case not found.");

        if (!CoordinatorMayView(userRole, c))
            return ApiResponse<CaseDto>.FailResponse("You don't have access to this case.");

        return ApiResponse<CaseDto>.SuccessResponse(c.ToDto());
    }

    public async Task<ApiResponse<CaseDto>> CreateCaseAsync(CreateCaseRequest request, string createdBy, Guid? submittedByUserId = null)
    {
        var caseNumber = await _unitOfWork.Cases.GenerateCaseNumberAsync();

        // Resolve category and its confidentiality
        Guid? categoryId = null;
        CaseCategory? category = null;
        if (!string.IsNullOrWhiteSpace(request.CategoryId) && Guid.TryParse(request.CategoryId, out var catGuid))
        {
            category = await _categoryRepo.GetByIdAsync(catGuid);
            if (category is not null)
                categoryId = category.Id;
        }

        var caseType = MappingExtensions.ParseEnum<CaseType>(request.Type);
        // If admin-marked category is confidential, force the case type to Confidential
        if (category?.IsConfidential == true)
            caseType = CaseType.Confidential;

        var newCase = new Case
        {
            Id = Guid.NewGuid(),
            CaseNumber = caseNumber,
            StudentName = request.StudentName,
            StudentId = request.StudentId,
            Type = caseType,
            Status = CaseStatus.Submitted,
            Priority = MappingExtensions.ParseEnum<Priority>(request.Priority),
            Description = request.Description,
            CategoryId = categoryId,
            IncidentLatitude = request.IncidentLatitude,
            IncidentLongitude = request.IncidentLongitude,
            IncidentLocationDescription = request.IncidentLocationDescription,
            Subject = request.Subject,
            StudentDepartment = request.StudentDepartment,
            StudentContact = request.StudentContact,
            StudentAdvisorName = request.StudentAdvisorName,
            StudentFatherName = request.StudentFatherName,
            StudentFatherContact = request.StudentFatherContact,
            AccusedName = request.AccusedName,
            AccusedId = request.AccusedId,
            AccusedDepartment = request.AccusedDepartment,
            AccusedContact = request.AccusedContact,
            AccusedGuardianContact = request.AccusedGuardianContact,
            VideoLink = request.VideoLink,
            IncidentDate = request.IncidentDate is not null ? DateTime.SpecifyKind(DateTime.Parse(request.IncidentDate), DateTimeKind.Utc) : null,
            SubmittedByUserId = submittedByUserId,
            SubmitterGender = string.IsNullOrWhiteSpace(request.Gender)
                ? Gender.Unspecified
                : MappingExtensions.ParseEnum<Gender>(request.Gender)
        };

        var timelineEvent = new TimelineEvent
        {
            Id = Guid.NewGuid(),
            CaseId = newCase.Id,
            Action = "Case Created",
            Description = $"Case {caseNumber} was created.",
            User = createdBy
        };

        newCase.TimelineEvents.Add(timelineEvent);

        if (request.Complainants?.Count > 0)
        {
            var order = 0;
            foreach (var c in request.Complainants)
            {
                newCase.Complainants.Add(new CaseComplainant
                {
                    Id = Guid.NewGuid(), CaseId = newCase.Id, Name = c.Name, StudentId = c.StudentId,
                    Department = c.Department, Contact = c.Contact, AdvisorName = c.AdvisorName,
                    FatherName = c.FatherName, FatherContact = c.FatherContact, Order = order++
                });
            }
        }

        if (request.AccusedPersons?.Count > 0)
        {
            var order = 0;
            foreach (var a in request.AccusedPersons)
            {
                newCase.AccusedPersons.Add(new CaseAccused
                {
                    Id = Guid.NewGuid(), CaseId = newCase.Id, Name = a.Name,
                    AccusedStudentId = a.AccusedStudentId, Department = a.Department,
                    Contact = a.Contact, GuardianContact = a.GuardianContact, Order = order++
                });
            }
        }

        // Gender-based coordinator routing for non-Type-1 cases: the complainant's gender
        // (chosen on the Type-2 form) decides which coordinator handles the case —
        // female → female coordinator, male → (male) coordinator. Confidential always goes
        // to a female coordinator. If no gender is supplied, fall back to the submitter's
        // account gender so existing flows keep working.
        User? routedCoordinator = null;
        string? routedRole = null;
        if (newCase.Type != CaseType.Type1)
        {
            var selectedGender = request.Gender?.Trim().ToLowerInvariant();
            if (newCase.Type == CaseType.Confidential)
                selectedGender = "female";
            if (string.IsNullOrEmpty(selectedGender) && submittedByUserId.HasValue)
            {
                var submitter = await _unitOfWork.Users.GetByIdAsync(submittedByUserId.Value);
                if (submitter?.Gender == Gender.Female) selectedGender = "female";
                else if (submitter?.Gender == Gender.Male) selectedGender = "male";
            }

            if (selectedGender == "female")
            {
                var fcs = await _unitOfWork.Users.FindAsync(u => u.Role == UserRole.FemaleCoordinator && u.IsActive);
                routedCoordinator = fcs.FirstOrDefault();
                routedRole = "female-coordinator";
            }
            else if (selectedGender == "male")
            {
                var cs = (await _unitOfWork.Users.FindAsync(u => u.Role == UserRole.Coordinator && u.IsActive)).ToList();
                routedCoordinator = cs.FirstOrDefault(u => u.Gender == Gender.Male) ?? cs.FirstOrDefault();
                routedRole = "coordinator";
            }
        }

        if (routedCoordinator is not null)
        {
            newCase.AssignedToId = routedCoordinator.Id;
            newCase.ForwardedToRole = routedRole;
            newCase.Assignments.Add(new CaseAssignment
            {
                Id = Guid.NewGuid(),
                CaseId = newCase.Id,
                UserId = routedCoordinator.Id,
                AssignedById = submittedByUserId,
                AssignedAt = DateTime.UtcNow,
                IsActive = true,
                IsPrimary = true
            });
        }

        await _unitOfWork.Cases.AddAsync(newCase);
        await _unitOfWork.SaveChangesAsync();

        // Notifications
        if (routedCoordinator is not null)
        {
            await _notificationService.CreateAsync(routedCoordinator.Id, null, "New Case Auto-Routed",
                $"Case {caseNumber} was auto-assigned to you.", newCase.Id);
        }

        if (newCase.Type == CaseType.Type1)
        {
            var settingResp = await _systemSettingService.GetSettingByKeyAsync("type1_forwarding_roles");
            var rolesCsv = settingResp?.Data?.Value;
            if (string.IsNullOrWhiteSpace(rolesCsv))
                rolesCsv = "proctor,deputy-proctor";

            var targetRoles = rolesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(r => r.Trim())
                                      .Where(r => !string.IsNullOrEmpty(r))
                                      .ToList();

            foreach (var roleKey in targetRoles)
            {
                await _notificationService.CreateAsync(null, roleKey, "New Type-1 Incident",
                    $"Case {caseNumber} has been submitted by {request.StudentName}.", newCase.Id);
            }

            if (targetRoles.Count > 0)
            {
                newCase.ForwardedToRole = targetRoles[0];
                _unitOfWork.Cases.Update(newCase);
                await _unitOfWork.SaveChangesAsync();
            }
        }
        else
        {
            // Notify the role queue too, so coordinators who aren't the specific assignee still see it.
            var targetRole = routedRole ?? (newCase.Type == CaseType.Confidential ? "female-coordinator" : "coordinator");
            await _notificationService.CreateAsync(null, targetRole, "New Case Submitted",
                $"Case {caseNumber} has been submitted by {request.StudentName}.", newCase.Id);
        }

        var created = await _unitOfWork.Cases.GetByIdWithDetailsAsync(newCase.Id);
        return ApiResponse<CaseDto>.SuccessResponse(created!.ToDto(), "Case created successfully.");
    }

    public async Task<ApiResponse<CaseDto>> UpdateCaseAsync(Guid id, UpdateCaseRequest request)
    {
        var c = await _unitOfWork.Cases.GetByIdWithDetailsAsync(id);
        if (c is null)
            return ApiResponse<CaseDto>.FailResponse("Case not found.");

        if (request.Priority is not null)
            c.Priority = MappingExtensions.ParseEnum<Priority>(request.Priority);

        if (request.Description is not null)
            c.Description = request.Description;

        if (request.CategoryId is not null)
        {
            if (Guid.TryParse(request.CategoryId, out var newCatId))
                c.CategoryId = newCatId;
            else if (string.IsNullOrWhiteSpace(request.CategoryId))
                c.CategoryId = null;
        }

        if (request.IncidentLatitude.HasValue) c.IncidentLatitude = request.IncidentLatitude;
        if (request.IncidentLongitude.HasValue) c.IncidentLongitude = request.IncidentLongitude;
        if (request.IncidentLocationDescription is not null) c.IncidentLocationDescription = request.IncidentLocationDescription;

        if (request.AssignedToId is not null && Guid.TryParse(request.AssignedToId, out var newAssignee))
        {
            c.AssignedToId = newAssignee;
            c.Status = CaseStatus.Assigned;
        }

        if (request.StudentDepartment is not null) c.StudentDepartment = request.StudentDepartment;
        if (request.StudentContact is not null) c.StudentContact = request.StudentContact;
        if (request.StudentAdvisorName is not null) c.StudentAdvisorName = request.StudentAdvisorName;
        if (request.StudentFatherName is not null) c.StudentFatherName = request.StudentFatherName;
        if (request.StudentFatherContact is not null) c.StudentFatherContact = request.StudentFatherContact;
        if (request.AccusedName is not null) c.AccusedName = request.AccusedName;
        if (request.AccusedId is not null) c.AccusedId = request.AccusedId;
        if (request.AccusedDepartment is not null) c.AccusedDepartment = request.AccusedDepartment;
        if (request.AccusedContact is not null) c.AccusedContact = request.AccusedContact;
        if (request.AccusedGuardianContact is not null) c.AccusedGuardianContact = request.AccusedGuardianContact;
        if (request.VideoLink is not null) c.VideoLink = request.VideoLink;
        if (request.IncidentDate is not null) c.IncidentDate = DateTime.Parse(request.IncidentDate).ToUniversalTime();

        if (request.Complainants is not null)
        {
            foreach (var existing in c.Complainants.ToList())
                _unitOfWork.Remove(existing);
            c.Complainants.Clear();
            var order = 0;
            foreach (var cc in request.Complainants)
            {
                var comp = new CaseComplainant
                {
                    Id = Guid.NewGuid(), CaseId = c.Id, Name = cc.Name, StudentId = cc.StudentId,
                    Department = cc.Department, Contact = cc.Contact, AdvisorName = cc.AdvisorName,
                    FatherName = cc.FatherName, FatherContact = cc.FatherContact, Order = order++
                };
                c.Complainants.Add(comp);
                _unitOfWork.Add(comp); // force INSERT (client-set Guid key)
            }
        }

        if (request.AccusedPersons is not null)
        {
            foreach (var existing in c.AccusedPersons.ToList())
                _unitOfWork.Remove(existing);
            c.AccusedPersons.Clear();
            var order = 0;
            foreach (var a in request.AccusedPersons)
            {
                var acc = new CaseAccused
                {
                    Id = Guid.NewGuid(), CaseId = c.Id, Name = a.Name,
                    AccusedStudentId = a.AccusedStudentId, Department = a.Department,
                    Contact = a.Contact, GuardianContact = a.GuardianContact, Order = order++
                };
                c.AccusedPersons.Add(acc);
                _unitOfWork.Add(acc); // force INSERT (client-set Guid key)
            }
        }

        // `c` is change-tracked; mutating it is enough. Do NOT force the whole graph to
        // EntityState.Modified via Update() — that makes EF emit UPDATEs for the freshly
        // added complainant/accused rows (which don't exist yet) → "affected 0 rows".
        c.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Add(new TimelineEvent
        {
            Id = Guid.NewGuid(),
            CaseId = c.Id,
            Action = "Case Updated",
            Description = "Case details were updated.",
            User = "System"
        });

        await _unitOfWork.SaveChangesAsync();

        if (request.AssignedToId is not null && Guid.TryParse(request.AssignedToId, out var notifyAssignee))
        {
            await _notificationService.CreateAsync(notifyAssignee, null,
                "Case Assigned", $"You have been assigned to case {c.CaseNumber}.", c.Id);
        }

        var updated = await _unitOfWork.Cases.GetByIdWithDetailsAsync(id);
        return ApiResponse<CaseDto>.SuccessResponse(updated!.ToDto(), "Case updated successfully.");
    }

    public async Task<ApiResponse<CaseDto>> AcknowledgeCaseAsync(Guid id, AcknowledgeCaseRequest request, Guid userId, string userName)
    {
        var c = await _unitOfWork.Cases.GetByIdWithDetailsAsync(id);
        if (c is null)
            return ApiResponse<CaseDto>.FailResponse("Case not found.");

        if (c.Type != CaseType.Type1)
            return ApiResponse<CaseDto>.FailResponse("Acknowledgment is only available for Type-1 cases.");

        c.IsAcknowledged = true;
        c.AcknowledgedAt = DateTime.UtcNow;
        c.AcknowledgedById = userId;
        c.AcknowledgedByName = userName;
        c.AcknowledgmentComment = request.Comment;

        _unitOfWork.Cases.Update(c);

        _unitOfWork.Add(new TimelineEvent
        {
            Id = Guid.NewGuid(),
            CaseId = c.Id,
            Action = "Case Acknowledged",
            Description = $"{userName} acknowledged the incident with note: \"{request.Comment}\"",
            User = userName
        });

        await _unitOfWork.SaveChangesAsync();

        if (c.SubmittedByUserId.HasValue)
        {
            var msg = string.IsNullOrWhiteSpace(request.Comment)
                ? $"Your case {c.CaseNumber} has been acknowledged by {userName} (Proctor)."
                : $"Message from {userName} (Proctor) on {c.CaseNumber}: \"{request.Comment}\"";
            await _notificationService.CreateAsync(c.SubmittedByUserId.Value, null,
                "Case Acknowledged — Message from Proctor", msg, c.Id);
        }

        var updated = await _unitOfWork.Cases.GetByIdWithDetailsAsync(id);
        return ApiResponse<CaseDto>.SuccessResponse(updated!.ToDto(), "Case acknowledged.");
    }

    public async Task<ApiResponse<CaseDto>> AssignCaseAsync(Guid id, AssignCaseRequest request, Guid actingUserId, string actingUserName)
    {
        var c = await _unitOfWork.Cases.GetByIdWithDetailsAsync(id);
        if (c is null)
            return ApiResponse<CaseDto>.FailResponse("Case not found.");

        var userGuids = request.UserIds
            .Select(u => Guid.TryParse(u, out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue).Select(g => g!.Value).Distinct().ToList();

        if (userGuids.Count == 0)
            return ApiResponse<CaseDto>.FailResponse("At least one user must be assigned.");

        Guid? primaryGuid = null;
        if (!string.IsNullOrWhiteSpace(request.PrimaryUserId) && Guid.TryParse(request.PrimaryUserId, out var pg))
            primaryGuid = pg;

        // Deactivate existing assignments not in the new list
        foreach (var existing in c.Assignments.Where(a => a.IsActive).ToList())
        {
            if (!userGuids.Contains(existing.UserId))
                existing.IsActive = false;
        }

        // Add new assignments for users not already active
        foreach (var uid in userGuids)
        {
            var existing = c.Assignments.FirstOrDefault(a => a.UserId == uid);
            if (existing is null)
            {
                var assignment = new CaseAssignment
                {
                    Id = Guid.NewGuid(),
                    CaseId = c.Id,
                    UserId = uid,
                    AssignedById = actingUserId,
                    AssignedAt = DateTime.UtcNow,
                    IsActive = true,
                    IsPrimary = primaryGuid.HasValue ? uid == primaryGuid.Value : false
                };
                c.Assignments.Add(assignment);
                // Force the new row to be tracked as Added → INSERT. Adding it only through the
                // parent's navigation collection lets EF treat the client-set Guid key as an
                // existing row and emit an UPDATE that matches 0 rows (the reported 500).
                _unitOfWork.Add(assignment);
            }
            else
            {
                existing.IsActive = true;
                existing.IsPrimary = primaryGuid.HasValue ? uid == primaryGuid.Value : existing.IsPrimary;
            }
        }

        if (primaryGuid.HasValue)
        {
            foreach (var a in c.Assignments)
                a.IsPrimary = a.UserId == primaryGuid.Value && a.IsActive;
            c.AssignedToId = primaryGuid;
        }
        else if (c.Assignments.Any(a => a.IsActive))
        {
            var firstActive = c.Assignments.First(a => a.IsActive);
            firstActive.IsPrimary = true;
            c.AssignedToId = firstActive.UserId;
        }

        if (c.Status == CaseStatus.Submitted || c.Status == CaseStatus.Verified)
            c.Status = CaseStatus.Assigned;

        // `c` was loaded with change-tracking, so mutating it (and adding/removing
        // CaseAssignment children) is already detected by EF. Do NOT call Update(), which
        // forces the whole graph to EntityState.Modified — that makes EF emit an UPDATE for
        // the newly-added assignment rows (client-assigned Guid keys) which don't exist yet,
        // causing the "expected to affect 1 row(s), but actually affected 0 row(s)" error.
        c.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Add(new TimelineEvent
        {
            Id = Guid.NewGuid(),
            CaseId = c.Id,
            Action = "Case Assigned",
            Description = $"{actingUserName} assigned case to {userGuids.Count} user(s).",
            User = actingUserName
        });

        await _unitOfWork.SaveChangesAsync();

        foreach (var uid in userGuids)
        {
            await _notificationService.CreateAsync(uid, null, "Case Assigned to You",
                $"You have been assigned to case {c.CaseNumber}.", c.Id);
        }

        var updated = await _unitOfWork.Cases.GetByIdWithDetailsAsync(id);
        return ApiResponse<CaseDto>.SuccessResponse(updated!.ToDto(), "Assignments updated.");
    }

    public async Task<ApiResponse<CaseDto>> UpdateCaseStatusAsync(Guid id, UpdateCaseStatusRequest request, string updatedBy, string userRole)
    {
        var c = await _unitOfWork.Cases.GetByIdWithDetailsAsync(id);
        if (c is null)
            return ApiResponse<CaseDto>.FailResponse("Case not found.");

        var oldStatus = c.Status;
        var newStatus = MappingExtensions.ParseEnum<CaseStatus>(request.Status);

        if (!await _workflowService.ValidateTransitionAsync(oldStatus, newStatus, userRole))
            return ApiResponse<CaseDto>.FailResponse($"Transition from '{oldStatus.ToKebabCase()}' to '{newStatus.ToKebabCase()}' is not allowed for role '{userRole}'.");

        c.Status = newStatus;

        if (!string.IsNullOrWhiteSpace(request.Verdict))
            c.Verdict = request.Verdict;

        if (!string.IsNullOrWhiteSpace(request.Recommendation))
            c.Recommendation = request.Recommendation;

        _unitOfWork.Cases.Update(c);

        _unitOfWork.Add(new TimelineEvent
        {
            Id = Guid.NewGuid(),
            CaseId = c.Id,
            Action = "Status Changed",
            Description = $"Status changed from {oldStatus.ToKebabCase()} to {newStatus.ToKebabCase()}.",
            User = updatedBy
        });

        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            _unitOfWork.Add(new Note
            {
                Id = Guid.NewGuid(),
                CaseId = c.Id,
                Content = request.Note,
                Author = updatedBy
            });
        }

        await _unitOfWork.SaveChangesAsync();

        if (newStatus == CaseStatus.ResubmissionRequested && c.SubmittedByUserId.HasValue)
        {
            await _notificationService.CreateAsync(c.SubmittedByUserId.Value, "student",
                "Resubmission Requested", $"Case {c.CaseNumber} needs changes. Please review the coordinator comments and resubmit.", c.Id);
        }
        else if (newStatus == CaseStatus.Rejected && c.SubmittedByUserId.HasValue)
        {
            await _notificationService.CreateAsync(c.SubmittedByUserId.Value, "student",
                "Case Rejected", $"Case {c.CaseNumber} has been rejected.", c.Id);
        }
        else if (newStatus == CaseStatus.Submitted)
        {
            var targetRole = c.Type == CaseType.Confidential ? "female-coordinator" : "coordinator";
            await _notificationService.CreateAsync(null, targetRole,
                "Case Resubmitted", $"Case {c.CaseNumber} has been resubmitted by {c.StudentName}.", c.Id);
        }
        else
        {
            var notifyRole = newStatus switch
            {
                CaseStatus.Verified => "proctor",
                CaseStatus.ForwardedToRegistrar => "registrar",
                CaseStatus.ForwardedToCommittee => "disciplinary-committee",
                CaseStatus.Resolved or CaseStatus.Closed => "proctor",
                _ => "proctor"
            };

            await _notificationService.CreateAsync(null, notifyRole,
                "Case Status Updated", $"Case {c.CaseNumber} status changed to {newStatus.ToKebabCase()}.", c.Id);
        }

        if (newStatus != CaseStatus.ResubmissionRequested && newStatus != CaseStatus.Rejected && newStatus != CaseStatus.Submitted && c.SubmittedByUserId.HasValue)
        {
            await _notificationService.CreateAsync(c.SubmittedByUserId.Value, null,
                "Case Update", $"Your case {c.CaseNumber} status changed to {newStatus.ToKebabCase()}.", c.Id);
        }

        var updated = await _unitOfWork.Cases.GetByIdWithDetailsAsync(id);
        return ApiResponse<CaseDto>.SuccessResponse(updated!.ToDto(), "Case status updated successfully.");
    }

    public async Task<ApiResponse<CaseDto>> ForwardCaseAsync(Guid id, ForwardCaseRequest request, string updatedBy, string userRole)
    {
        var c = await _unitOfWork.Cases.GetByIdWithDetailsAsync(id);
        if (c is null)
            return ApiResponse<CaseDto>.FailResponse("Case not found.");

        var newStatus = await _workflowService.GetForwardStatusAsync(userRole, request.TargetRole, c.Status);
        if (newStatus is null)
            return ApiResponse<CaseDto>.FailResponse($"Cannot forward case from role '{userRole}' to '{request.TargetRole}'.");

        var oldStatus = c.Status;
        c.Status = newStatus.Value;
        c.ForwardedToRole = request.TargetRole;

        if (!string.IsNullOrWhiteSpace(request.AssignedToUserId) && Guid.TryParse(request.AssignedToUserId, out var assigneeId))
            c.AssignedToId = assigneeId;

        if (!string.IsNullOrWhiteSpace(request.Recommendation))
            c.Recommendation = request.Recommendation;

        if (!string.IsNullOrWhiteSpace(request.Verdict))
            c.Verdict = request.Verdict;

        _unitOfWork.Cases.Update(c);

        _unitOfWork.Add(new TimelineEvent
        {
            Id = Guid.NewGuid(),
            CaseId = c.Id,
            Action = "Case Forwarded",
            Description = $"Case forwarded from {userRole} to {request.TargetRole}. Status changed from {oldStatus.ToKebabCase()} to {newStatus.Value.ToKebabCase()}.",
            User = updatedBy
        });

        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            _unitOfWork.Add(new Note
            {
                Id = Guid.NewGuid(),
                CaseId = c.Id,
                Content = request.Note,
                Author = updatedBy
            });
        }

        await _unitOfWork.SaveChangesAsync();

        await _notificationService.CreateAsync(null, request.TargetRole,
            "Case Forwarded to You", $"Case {c.CaseNumber} has been forwarded to your attention by {updatedBy}.", c.Id);

        if (!string.IsNullOrWhiteSpace(request.AssignedToUserId) && Guid.TryParse(request.AssignedToUserId, out var notifyUserId))
        {
            await _notificationService.CreateAsync(notifyUserId, null,
                "Case Assigned to You", $"Case {c.CaseNumber} has been specifically assigned to you.", c.Id);
        }

        var updated = await _unitOfWork.Cases.GetByIdWithDetailsAsync(id);
        return ApiResponse<CaseDto>.SuccessResponse(updated!.ToDto(), "Case forwarded successfully.");
    }

    public async Task<ApiResponse<ReportDto>> CreateReportAsync(Guid caseId, CreateReportRequest request, string createdByName, Guid createdById)
    {
        var c = await _unitOfWork.Cases.GetByIdWithDetailsAsync(caseId);
        if (c is null)
            return ApiResponse<ReportDto>.FailResponse("Case not found.");

        var report = new Report
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            Content = request.Content,
            CreatedByName = createdByName,
            CreatedById = createdById,
            IsDraft = request.IsDraft,
            IsFinal = request.IsFinal,
            SectionsJson = request.SectionsJson
        };

        _unitOfWork.Add(report);

        _unitOfWork.Add(new TimelineEvent
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            Action = request.IsDraft ? "Draft Report Created" : "Report Submitted",
            Description = $"{(request.IsDraft ? "Draft report" : "Report")} created by {createdByName}.",
            User = createdByName
        });

        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<ReportDto>.SuccessResponse(report.ToDto(), "Report created successfully.");
    }

    public async Task<ApiResponse<List<ReportDto>>> GetReportsAsync(Guid caseId)
    {
        var c = await _unitOfWork.Cases.GetByIdWithDetailsAsync(caseId);
        if (c is null)
            return ApiResponse<List<ReportDto>>.FailResponse("Case not found.");

        var reports = c.Reports.Select(r => r.ToDto()).ToList();
        return ApiResponse<List<ReportDto>>.SuccessResponse(reports);
    }

    public async Task<ApiResponse<ReportDto>> UpdateReportAsync(Guid caseId, Guid reportId, CreateReportRequest request, string updatedByName)
    {
        var c = await _unitOfWork.Cases.GetByIdWithDetailsAsync(caseId);
        if (c is null) return ApiResponse<ReportDto>.FailResponse("Case not found.");
        var report = c.Reports.FirstOrDefault(r => r.Id == reportId);
        if (report is null) return ApiResponse<ReportDto>.FailResponse("Report not found.");

        report.Content = request.Content;
        report.IsDraft = request.IsDraft;
        report.IsFinal = request.IsFinal;
        report.SectionsJson = request.SectionsJson;
        report.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();
        return ApiResponse<ReportDto>.SuccessResponse(report.ToDto(), "Report updated.");
    }

    public async Task<ApiResponse<bool>> DeleteCaseAsync(Guid id)
    {
        var c = await _unitOfWork.Cases.GetByIdWithDetailsAsync(id);
        if (c is null)
            return ApiResponse<bool>.FailResponse("Case not found.");

        _unitOfWork.Cases.Remove(c);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true, "Case deleted successfully.");
    }

    private static Expression<Func<Case, bool>> BuildMyCasesFilter(Guid userId, string userRole)
    {
        if (userRole == "student")
            return c => c.SubmittedByUserId == userId;

        return c => c.AssignedToId == userId
                 || c.ForwardedToRole == userRole
                 || c.SubmittedByUserId == userId
                 || c.Assignments.Any(a => a.UserId == userId && a.IsActive);
    }

    public async Task<ApiResponse<PagedResult<CaseListDto>>> GetMyCasesAsync(Guid userId, string userRole, int page, int pageSize)
    {
        var filter = BuildMyCasesFilter(userId, userRole);
        var allCases = await _unitOfWork.Cases.FindWithDetailsAsync(filter);

        var totalCount = allCases.Count();
        var items = allCases
            .OrderByDescending(c => c.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => c.ToListDto());

        var result = new PagedResult<CaseListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return ApiResponse<PagedResult<CaseListDto>>.SuccessResponse(result);
    }

    public async Task<ApiResponse<int>> GetMyCasesCountAsync(Guid userId, string userRole)
    {
        var filter = BuildMyCasesFilter(userId, userRole);
        var count = await _unitOfWork.Cases.CountAsync(filter);
        return ApiResponse<int>.SuccessResponse(count);
    }
}
