using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROCTOR.API.Services;
using PROCTOR.Application.Common;
using PROCTOR.Application.Interfaces;
using PROCTOR.Application.Mapping;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.API.Controllers;

[ApiController]
[Route("api/type3")]
[Authorize]
public class Type3WorkflowController : ControllerBase
{
    private readonly ProctorDbContext _db;
    private readonly IPermissionChecker _permissions;

    public Type3WorkflowController(ProctorDbContext db, IPermissionChecker permissions)
    {
        _db = db;
        _permissions = permissions;
    }

    private string Role => (User.FindFirst("role")?.Value ?? "").NormalizeRoleKey();
    private string Name => User.FindFirst("name")?.Value ?? "Unknown";
    private Guid UserId => Guid.TryParse(User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    [HttpPost("cases/{caseId:guid}/start")]
    public async Task<IActionResult> Start(Guid caseId, [FromBody] StartType3Request request)
    {
        if (!await _permissions.HasPermissionAsync(Role, "completed-reports", "send"))
            return StatusCode(403, ApiResponse<object>.FailResponse("Completed Reports → Send permission is required."));
        if (string.IsNullOrWhiteSpace(request.Remarks))
            return BadRequest(ApiResponse<object>.FailResponse("Remarks are required."));
        if (request.ReportId == Guid.Empty)
            return BadRequest(ApiResponse<object>.FailResponse("Select a finalized report to send."));

        var c = await _db.Cases.Include(x => x.Reports).Include(x => x.Type3Workflow).FirstOrDefaultAsync(x => x.Id == caseId);
        if (c is null) return NotFound(ApiResponse<object>.FailResponse("Case not found."));
        var report = c.Reports.SingleOrDefault(x => x.Id == request.ReportId && x.IsFinal);
        if (report is null) return BadRequest(ApiResponse<object>.FailResponse("The selected finalized report was not found for this case."));
        if (c.Type3Workflow is not null || c.Type == CaseType.Type3) return Conflict(ApiResponse<object>.FailResponse("This case is already in the Type-3 workflow."));
        if (c.Type != CaseType.Type2 && c.Type != CaseType.Confidential && !c.IsConfidential)
            return BadRequest(ApiResponse<object>.FailResponse("Only finalized Type-2 or confidential cases can be sent to Type-3."));

        var now = DateTime.UtcNow;
        if (c.Type == CaseType.Confidential) c.IsConfidential = true;
        c.Type = CaseType.Type3;
        var workflow = new Type3Workflow
        {
            Id = Guid.NewGuid(), CaseId = c.Id, ReportId = report.Id, CurrentStage = Type3WorkflowStage.RegistrarReview,
            StartedById = UserId, StartedByName = Name, StartedAt = now, CreatedAt = now, UpdatedAt = now
        };
        _db.Type3Workflows.Add(workflow);
        AddTransition(workflow, Type3WorkflowStage.RegistrarReview, Type3WorkflowStage.RegistrarReview, "registrar", request.Remarks);
        AddNotification(null, "registrar", "New Report Received", "A new finalized report is waiting in Registrar Reports.");
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessResponse(new { caseId = c.Id, reportId = report.Id, stage = "registrar-review", statusLabel = "Sent to Registrar" }));
    }

    [HttpGet("queue")]
    public async Task<IActionResult> Queue()
    {
        var menuKey = MenuKeyForRole(Role);
        if (menuKey is null || !await _permissions.HasPermissionAsync(Role, menuKey, "read")) return Forbid();
        var query = _db.Type3Workflows.AsNoTracking()
            .Include(x => x.Case)
            .Include(x => x.Report)
            .Include(x => x.Transitions)
            .Include(x => x.MemberRemarks)
            .AsQueryable();

        // A role's reports page is both its active inbox and its forwarding history. Once a
        // report reaches a role, keep it visible there after that role forwards it onward.
        query = Role switch
        {
            "registrar" => query.Where(x => x.Transitions.Any(t => t.TargetRole == "registrar" || t.ActorRole == "registrar")),
            "vc" => query.Where(x => x.Transitions.Any(t => t.TargetRole == "vc" || t.ActorRole == "vc")),
            "dc-chairman" => query.Where(x => x.Transitions.Any(t => t.TargetRole == "dc-chairman" || t.ActorRole == "dc-chairman")),
            "dc-member" => query.Where(x => x.MemberRemarks.Any(r => r.MemberUserId == UserId)),
            "dc-secretary" => query.Where(x => x.Transitions.Any(t => t.TargetRole == "dc-secretary" || t.ActorRole == "dc-secretary")),
            "chairman" => query.Where(x => x.Transitions.Any(t => t.TargetRole == "chairman" || t.ActorRole == "chairman")),
            "super-admin" => query,
            _ => query.Where(_ => false)
        };

        var workflows = await query.OrderByDescending(x => x.UpdatedAt).ToListAsync();

        var resolutionByCase = await _db.DisciplinaryResolutionCases.AsNoTracking()
            .Include(x => x.Resolution).ToDictionaryAsync(x => x.CaseId);
        var rows = workflows.Select(w => ToWorkflowDto(w, resolutionByCase.GetValueOrDefault(w.CaseId))).ToList();
        return Ok(ApiResponse<object>.SuccessResponse(rows));
    }

    [HttpPost("cases/{caseId:guid}/forward")]
    public async Task<IActionResult> ForwardCase(Guid caseId, [FromBody] RemarksRequest request)
    {
        var menuKey = MenuKeyForRole(Role);
        if (menuKey is null || !await _permissions.HasPermissionAsync(Role, menuKey, "update")) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Remarks)) return BadRequest(ApiResponse<object>.FailResponse("Remarks are required."));
        var workflow = await _db.Type3Workflows.AsNoTracking()
            .Include(x => x.Case)
            .Include(x => x.MemberRemarks)
            .FirstOrDefaultAsync(x => x.CaseId == caseId);
        if (workflow is null) return NotFound(ApiResponse<object>.FailResponse("Type-3 workflow not found."));

        Type3WorkflowStage next;
        string target;
        if ((Role == "registrar" || Role == "super-admin") && workflow.CurrentStage == Type3WorkflowStage.RegistrarReview)
            (next, target) = (Type3WorkflowStage.VcReview, "vc");
        else if ((Role == "vc" || Role == "super-admin") && workflow.CurrentStage == Type3WorkflowStage.VcReview)
            (next, target) = (Type3WorkflowStage.DcChairmanReview, "dc-chairman");
        else if ((Role == "dc-chairman" || Role == "super-admin") && workflow.CurrentStage == Type3WorkflowStage.DcChairmanReview)
            (next, target) = (Type3WorkflowStage.DcMemberReview, "dc-member");
        else return Conflict(ApiResponse<object>.FailResponse("This case is not at the stage your role can forward."));

        List<User> members = [];
        if (next == Type3WorkflowStage.DcMemberReview)
        {
            members = await _db.Users.AsNoTracking().Where(x => x.IsActive && x.Role == UserRole.DCMember).OrderBy(x => x.Email).ToListAsync();
            if (members.Count != 6) return BadRequest(ApiResponse<object>.FailResponse($"Exactly 6 active DC Members are required; currently {members.Count} are active."));
        }

        var from = workflow.CurrentStage;
        await using var transaction = await _db.Database.BeginTransactionAsync();

        // Compare-and-set makes this transition idempotent. A double click or a stale browser
        // can no longer execute the same stage twice or create duplicate recipient records.
        var affected = await _db.Type3Workflows
            .Where(x => x.Id == workflow.Id && x.CurrentStage == from)
            .ExecuteUpdateAsync(update => update
                .SetProperty(x => x.CurrentStage, next)
                .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));
        if (affected == 0)
        {
            await transaction.RollbackAsync();
            return Conflict(ApiResponse<object>.FailResponse("This report was already updated by another action. Refresh the page to see its current stage."));
        }

        // Repair any legacy workflow created before Type-3 case tagging was enforced, without
        // tracking a stale Case entity in the same write operation.
        await _db.Cases.Where(x => x.Id == caseId && x.Type != CaseType.Type3)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Type, CaseType.Type3));

        if (next == Type3WorkflowStage.DcMemberReview)
        {
            foreach (var member in members)
            {
                _db.DcMemberRemarks.Add(new DcMemberRemark { Id = Guid.NewGuid(), WorkflowId = workflow.Id, MemberUserId = member.Id, MemberName = member.Name });
                AddNotification(member.Id, null, "Report Review Required", "A disciplinary report requires your remark in DC Members Reports.");
            }
        }
        else AddNotification(null, target, "Report Forwarded to You", $"A disciplinary report is waiting in {ReportQueueName(target)}.");

        AddTransition(workflow, from, next, target, request.Remarks);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(ApiResponse<object>.SuccessResponse(new { stage = next.ToKebabCase(), statusLabel = StageLabel(next) }));
    }

    [HttpPut("cases/{caseId:guid}/remark")]
    public async Task<IActionResult> SaveMemberRemark(Guid caseId, [FromBody] RemarksRequest request)
    {
        if (Role != "dc-member") return Forbid();
        if (!await _permissions.HasPermissionAsync(Role, "dc-member-reports", "update")) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Remarks)) return BadRequest(ApiResponse<object>.FailResponse("A remark is required."));
        var workflow = await _db.Type3Workflows.Include(x => x.Case).Include(x => x.MemberRemarks).FirstOrDefaultAsync(x => x.CaseId == caseId);
        if (workflow is null) return NotFound(ApiResponse<object>.FailResponse("Type-3 workflow not found."));
        if (workflow.CurrentStage != Type3WorkflowStage.DcMemberReview) return Conflict(ApiResponse<object>.FailResponse("Member remarks are closed for this case."));
        var remark = workflow.MemberRemarks.SingleOrDefault(x => x.MemberUserId == UserId);
        if (remark is null) return Forbid();
        if (remark.LockedAt.HasValue) return Conflict(ApiResponse<object>.FailResponse("This remark has been locked by a resolution batch."));
        remark.Content = request.Remarks.Trim();
        remark.SubmittedAt = DateTime.UtcNow;
        remark.UpdatedAt = DateTime.UtcNow;

        if (workflow.MemberRemarks.Count == 6 && workflow.MemberRemarks.All(x => x.SubmittedAt.HasValue || x.Id == remark.Id))
        {
            var from = workflow.CurrentStage;
            workflow.CurrentStage = Type3WorkflowStage.DcSecretaryReview;
            workflow.UpdatedAt = DateTime.UtcNow;
            AddTransition(workflow, from, workflow.CurrentStage, "dc-secretary", "All six DC Member remarks were submitted.");
            AddNotification(null, "dc-secretary", "Report Ready for Resolution", "A disciplinary report has all six member remarks and is ready in DCS Reports.");
        }
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessResponse(new { submitted = true }));
    }

    [HttpPost("resolutions")]
    public async Task<IActionResult> CreateResolution([FromBody] CreateResolutionRequest request)
    {
        if (Role != "dc-secretary" && Role != "super-admin") return Forbid();
        if (!await _permissions.HasPermissionAsync(Role, "dcs-reports", "create")) return Forbid();
        if (request.Cases.Count == 0) return BadRequest(ApiResponse<object>.FailResponse("Select at least one case."));
        if (request.Cases.Any(x => string.IsNullOrWhiteSpace(x.ShortDescription) || string.IsNullOrWhiteSpace(x.SecretaryRemarks)))
            return BadRequest(ApiResponse<object>.FailResponse("Short description and secretary remarks are required for every case."));
        var ids = request.Cases.Select(x => x.CaseId).Distinct().ToList();
        if (ids.Count != request.Cases.Count) return BadRequest(ApiResponse<object>.FailResponse("A case can only appear once in a resolution."));
        var workflows = await _db.Type3Workflows.Include(x => x.Case).Include(x => x.MemberRemarks).Where(x => ids.Contains(x.CaseId)).ToListAsync();
        if (workflows.Count != ids.Count || workflows.Any(x => x.CurrentStage != Type3WorkflowStage.DcSecretaryReview || x.MemberRemarks.Count != 6 || x.MemberRemarks.Any(r => !r.SubmittedAt.HasValue)))
            return Conflict(ApiResponse<object>.FailResponse("Every selected case must be awaiting resolution with all six remarks complete."));
        if (await _db.DisciplinaryResolutionCases.AnyAsync(x => ids.Contains(x.CaseId)))
            return Conflict(ApiResponse<object>.FailResponse("One or more selected cases already belong to a resolution."));

        var year = DateTime.UtcNow.Year;
        var count = await _db.DisciplinaryResolutions.CountAsync(x => x.CreatedAt.Year == year) + 1;
        var resolution = new DisciplinaryResolution
        {
            Id = Guid.NewGuid(), ResolutionNumber = $"RES-{year}-{count:D3}", Status = ResolutionStatus.Draft,
            CreatedById = UserId, CreatedByName = Name
        };
        var order = 1;
        foreach (var input in request.Cases)
        {
            resolution.Cases.Add(new DisciplinaryResolutionCase
            {
                Id = Guid.NewGuid(), CaseId = input.CaseId, DisplayOrder = order++,
                ShortDescription = input.ShortDescription.Trim(), SecretaryRemarks = input.SecretaryRemarks.Trim()
            });
            foreach (var remark in workflows.Single(x => x.CaseId == input.CaseId).MemberRemarks) remark.LockedAt = DateTime.UtcNow;
        }
        _db.DisciplinaryResolutions.Add(resolution);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessResponse(new { id = resolution.Id, resolution.ResolutionNumber, status = "draft" }, "Resolution created."));
    }

    [HttpPost("resolutions/{resolutionId:guid}/forward")]
    public async Task<IActionResult> ForwardResolution(Guid resolutionId, [FromBody] RemarksRequest request)
    {
        var menuKey = MenuKeyForRole(Role);
        if (menuKey is null || !await _permissions.HasPermissionAsync(Role, menuKey, "update")) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Remarks)) return BadRequest(ApiResponse<object>.FailResponse("Remarks are required."));
        var resolution = await ResolutionQuery().FirstOrDefaultAsync(x => x.Id == resolutionId);
        if (resolution is null) return NotFound(ApiResponse<object>.FailResponse("Resolution not found."));
        Type3WorkflowStage expected, next;
        ResolutionStatus nextStatus;
        string target;
        if ((Role == "dc-secretary" || Role == "super-admin") && resolution.Status == ResolutionStatus.Draft)
            (expected, next, nextStatus, target) = (Type3WorkflowStage.DcSecretaryReview, Type3WorkflowStage.ResolutionDcChairmanReview, ResolutionStatus.PendingDcChairman, "dc-chairman");
        else if ((Role == "dc-chairman" || Role == "super-admin") && resolution.Status == ResolutionStatus.PendingDcChairman)
            (expected, next, nextStatus, target) = (Type3WorkflowStage.ResolutionDcChairmanReview, Type3WorkflowStage.ChairmanReview, ResolutionStatus.PendingChairman, "chairman");
        else return Conflict(ApiResponse<object>.FailResponse("This resolution is not at the stage your role can forward."));
        if (resolution.Cases.Any(x => x.Case.Type3Workflow?.CurrentStage != expected))
            return Conflict(ApiResponse<object>.FailResponse("Resolution cases are not at the expected workflow stage."));

        resolution.Status = nextStatus;
        resolution.ForwardRemarks = request.Remarks.Trim();
        resolution.UpdatedAt = DateTime.UtcNow;
        foreach (var item in resolution.Cases)
        {
            var workflow = item.Case.Type3Workflow!;
            var from = workflow.CurrentStage;
            workflow.CurrentStage = next;
            workflow.UpdatedAt = DateTime.UtcNow;
            AddTransition(workflow, from, next, target, request.Remarks);
        }
        AddNotification(null, target, "Resolution Forwarded", $"Resolution {resolution.ResolutionNumber} requires your review.");
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessResponse(new { status = nextStatus.ToKebabCase() }));
    }

    [HttpPost("resolutions/{resolutionId:guid}/approve")]
    public async Task<IActionResult> ApproveResolution(Guid resolutionId, [FromBody] RemarksRequest request)
    {
        if (Role != "chairman" && Role != "super-admin") return Forbid();
        if (!await _permissions.HasPermissionAsync(Role, "chairman-reports", "update")) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Remarks)) return BadRequest(ApiResponse<object>.FailResponse("Approval remarks are required."));
        var resolution = await ResolutionQuery().FirstOrDefaultAsync(x => x.Id == resolutionId);
        if (resolution is null) return NotFound(ApiResponse<object>.FailResponse("Resolution not found."));
        if (resolution.Status != ResolutionStatus.PendingChairman) return Conflict(ApiResponse<object>.FailResponse("This resolution is not awaiting Chairman approval."));
        resolution.Status = ResolutionStatus.Approved;
        resolution.ApprovedById = UserId;
        resolution.ApprovedByName = Name;
        resolution.ApprovedAt = DateTime.UtcNow;
        resolution.UpdatedAt = DateTime.UtcNow;
        foreach (var item in resolution.Cases)
        {
            var workflow = item.Case.Type3Workflow!;
            var from = workflow.CurrentStage;
            workflow.CurrentStage = Type3WorkflowStage.Completed;
            workflow.CompletedAt = DateTime.UtcNow;
            workflow.UpdatedAt = DateTime.UtcNow;
            AddTransition(workflow, from, Type3WorkflowStage.Completed, "completed", request.Remarks);
        }
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessResponse(new { status = "approved" }));
    }

    [HttpGet("resolutions/{resolutionId:guid}/docx")]
    public async Task<IActionResult> DownloadDocx(Guid resolutionId)
    {
        if (Role is not ("dc-secretary" or "dc-chairman" or "chairman" or "super-admin")) return Forbid();
        var menuKey = MenuKeyForRole(Role);
        if (menuKey is null || !await _permissions.HasPermissionAsync(Role, menuKey, "read")) return Forbid();
        var resolution = await ResolutionQuery().AsNoTracking().FirstOrDefaultAsync(x => x.Id == resolutionId);
        if (resolution is null) return NotFound();
        var bytes = ResolutionDocxBuilder.Build(resolution);
        return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"{resolution.ResolutionNumber}.docx");
    }

    private IQueryable<DisciplinaryResolution> ResolutionQuery() => _db.DisciplinaryResolutions
        .Include(x => x.Cases).ThenInclude(x => x.Case).ThenInclude(x => x.Type3Workflow).ThenInclude(x => x!.MemberRemarks);

    private object ToWorkflowDto(Type3Workflow w, DisciplinaryResolutionCase? resolutionCase)
    {
        var ownRemark = w.MemberRemarks.FirstOrDefault(x => x.MemberUserId == UserId);
        return new
        {
            id = w.Id, caseId = w.CaseId, w.Case.CaseNumber, w.Case.StudentName, w.Case.StudentId,
            reportId = w.ReportId, reportContent = w.Report.Content, reportCreatedByName = w.Report.CreatedByName,
            reportCreatedDate = w.Report.CreatedAt,
            type = "type-3", isConfidential = w.Case.IsConfidential, stage = w.CurrentStage.ToKebabCase(),
            statusLabel = StageLabel(w.CurrentStage), reportFinalized = w.Report.IsFinal,
            submittedRemarks = w.MemberRemarks.Count(x => x.SubmittedAt.HasValue), requiredRemarks = 6,
            ownRemark = ownRemark?.Content, ownRemarkLocked = ownRemark?.LockedAt.HasValue ?? false,
            remarks = Role is "dc-secretary" or "dc-chairman" or "chairman" or "super-admin"
                ? w.MemberRemarks.Where(x => x.SubmittedAt.HasValue).Select(x => new { x.MemberName, x.Content, x.SubmittedAt }) : null,
            transitions = w.Transitions.OrderBy(x => x.CreatedAt).Select(x => new { from = x.FromStage.ToKebabCase(), to = x.ToStage.ToKebabCase(), x.ActorName, x.ActorRole, x.TargetRole, x.Remarks, x.CreatedAt }),
            resolution = resolutionCase is null ? null : new { id = resolutionCase.ResolutionId, number = resolutionCase.Resolution.ResolutionNumber, status = resolutionCase.Resolution.Status.ToKebabCase() }
        };
    }

    private static string StageLabel(Type3WorkflowStage stage) => stage switch
    {
        Type3WorkflowStage.RegistrarReview => "Sent to Registrar", Type3WorkflowStage.VcReview => "Forwarded to VC",
        Type3WorkflowStage.DcChairmanReview => "Forwarded to DC Chairman", Type3WorkflowStage.DcMemberReview => "Awaiting DC Member Remarks",
        Type3WorkflowStage.DcSecretaryReview => "Awaiting Resolution", Type3WorkflowStage.ResolutionDcChairmanReview => "Resolution Sent to DC Chairman",
        Type3WorkflowStage.ChairmanReview => "Resolution Sent to Chairman", Type3WorkflowStage.Completed => "Resolution Approved", _ => stage.ToString()
    };

    private static string? MenuKeyForRole(string role) => role switch
    {
        "registrar" => "registrar-reports", "vc" => "vc-reports", "dc-chairman" => "dc-reports",
        "dc-member" => "dc-member-reports", "dc-secretary" => "dcs-reports", "chairman" => "chairman-reports",
        "super-admin" => "settings", _ => null
    };

    private static string ReportQueueName(string role) => role switch
    {
        "vc" => "VC Reports", "dc-chairman" => "DC Reports", "dc-member" => "DC Members Reports",
        "dc-secretary" => "DCS Reports", "chairman" => "Chairman Reports", _ => "the reports queue"
    };

    private void AddTransition(Type3Workflow workflow, Type3WorkflowStage from, Type3WorkflowStage to, string target, string remarks) =>
        _db.Type3WorkflowTransitions.Add(new Type3WorkflowTransition
        {
            Id = Guid.NewGuid(), WorkflowId = workflow.Id, FromStage = from, ToStage = to,
            ActorUserId = UserId, ActorName = Name, ActorRole = Role, TargetRole = target, Remarks = remarks.Trim()
        });

    private void AddNotification(Guid? userId, string? role, string title, string message) =>
        _db.Notifications.Add(new Notification { Id = Guid.NewGuid(), UserId = userId, Role = role, Title = title, Message = message });
}

public sealed class RemarksRequest
{
    public string Remarks { get; set; } = string.Empty;
}
public sealed class StartType3Request
{
    public Guid ReportId { get; set; }
    public string Remarks { get; set; } = string.Empty;
}
public sealed class CreateResolutionRequest { public List<CreateResolutionCaseRequest> Cases { get; set; } = []; }
public sealed class CreateResolutionCaseRequest
{
    public Guid CaseId { get; set; }
    public string ShortDescription { get; set; } = string.Empty;
    public string SecretaryRemarks { get; set; } = string.Empty;
}
