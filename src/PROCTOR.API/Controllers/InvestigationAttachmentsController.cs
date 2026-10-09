using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROCTOR.Application.Common;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Infrastructure.Data;

namespace PROCTOR.API.Controllers;

[ApiController]
[Route("api/cases/{caseId:guid}/investigation-attachments")]
[Authorize]
public class InvestigationAttachmentsController : ControllerBase
{
    private static readonly Dictionary<string, string> AllowedImages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg", ["image/png"] = ".png", ["image/webp"] = ".webp"
    };
    private readonly ProctorDbContext _db;
    private readonly IWebHostEnvironment _env;

    public InvestigationAttachmentsController(ProctorDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    private string Role => User.FindFirst("role")?.Value ?? "";
    private string Name => User.FindFirst("name")?.Value ?? "Unknown";
    private Guid UserId => Guid.TryParse(User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    [HttpGet]
    public async Task<IActionResult> List(Guid caseId)
    {
        var access = await CheckAccess(caseId, "investigation_attachment_view_roles");
        if (access.NotFound) return NotFound(ApiResponse<object>.FailResponse("Case not found."));
        if (!access.Allowed) return Forbid();
        var rows = await _db.InvestigationAttachments.AsNoTracking().Where(x => x.CaseId == caseId)
            .OrderByDescending(x => x.CreatedAt).Select(x => new
            {
                id = x.Id, x.Name, x.Kind, x.ExternalUrl, x.ContentType, x.FileSize,
                x.UploadedByName, x.UploadedByRole, uploadedAt = x.CreatedAt,
                contentUrl = x.Kind == "image" ? $"/api/cases/{caseId}/investigation-attachments/{x.Id}/content" : null
            }).ToListAsync();
        return Ok(ApiResponse<object>.SuccessResponse(rows));
    }

    [HttpPost("image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(Guid caseId, [FromForm] IFormFile file, [FromForm] string? name)
    {
        var access = await CheckAccess(caseId, "investigation_attachment_upload_roles");
        if (access.NotFound) return NotFound(ApiResponse<object>.FailResponse("Case not found."));
        if (!access.Allowed) return Forbid();
        if (file is null || file.Length == 0 || !AllowedImages.TryGetValue(file.ContentType, out var extension))
            return BadRequest(ApiResponse<object>.FailResponse("Upload a JPEG, PNG, or WebP image."));
        if (file.Length > 10 * 1024 * 1024) return BadRequest(ApiResponse<object>.FailResponse("The image cannot exceed 10 MB."));
        if (!await HasValidImageSignature(file, file.ContentType.ToLowerInvariant()))
            return BadRequest(ApiResponse<object>.FailResponse("The uploaded file content does not match its image type."));

        var directory = Path.Combine(_env.ContentRootPath, "private_uploads", "investigations");
        Directory.CreateDirectory(directory);
        var storageName = $"{Guid.NewGuid():N}{extension}";
        await using (var stream = new FileStream(Path.Combine(directory, storageName), FileMode.CreateNew))
            await file.CopyToAsync(stream);
        var attachment = NewAttachment(caseId, SafeName(string.IsNullOrWhiteSpace(name) ? file.FileName : name), "image");
        attachment.StorageName = storageName;
        attachment.ContentType = file.ContentType;
        attachment.FileSize = file.Length;
        _db.InvestigationAttachments.Add(attachment);
        AddTimeline(caseId, $"Investigation image '{attachment.Name}' uploaded.");
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessResponse(new { id = attachment.Id, attachment.Name, attachment.Kind }));
    }

    [HttpPost("drive-link")]
    public async Task<IActionResult> AddDriveLink(Guid caseId, [FromBody] AddDriveLinkRequest request)
    {
        var access = await CheckAccess(caseId, "investigation_attachment_upload_roles");
        if (access.NotFound) return NotFound(ApiResponse<object>.FailResponse("Case not found."));
        if (!access.Allowed) return Forbid();
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            (uri.Host != "drive.google.com" && uri.Host != "docs.google.com"))
            return BadRequest(ApiResponse<object>.FailResponse("Enter a valid HTTPS Google Drive sharing link."));
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(ApiResponse<object>.FailResponse("A link name is required."));
        var attachment = NewAttachment(caseId, SafeName(request.Name), "drive-link");
        attachment.ExternalUrl = uri.ToString();
        _db.InvestigationAttachments.Add(attachment);
        AddTimeline(caseId, $"Investigation Drive link '{attachment.Name}' added.");
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessResponse(new { id = attachment.Id, attachment.Name, attachment.Kind, attachment.ExternalUrl }));
    }

    [HttpGet("{attachmentId:guid}/content")]
    public async Task<IActionResult> Download(Guid caseId, Guid attachmentId)
    {
        var access = await CheckAccess(caseId, "investigation_attachment_view_roles");
        if (access.NotFound) return NotFound();
        if (!access.Allowed) return Forbid();
        var attachment = await _db.InvestigationAttachments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == attachmentId && x.CaseId == caseId && x.Kind == "image");
        if (attachment?.StorageName is null) return NotFound();
        var path = Path.Combine(_env.ContentRootPath, "private_uploads", "investigations", Path.GetFileName(attachment.StorageName));
        if (!System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, attachment.ContentType ?? "application/octet-stream", enableRangeProcessing: true);
    }

    [HttpGet("access")]
    public async Task<IActionResult> Access(Guid caseId)
    {
        var view = await CheckAccess(caseId, "investigation_attachment_view_roles");
        var upload = await CheckAccess(caseId, "investigation_attachment_upload_roles");
        if (view.NotFound) return NotFound(ApiResponse<object>.FailResponse("Case not found."));
        return Ok(ApiResponse<object>.SuccessResponse(new { canView = view.Allowed, canUpload = upload.Allowed }));
    }

    private InvestigationAttachment NewAttachment(Guid caseId, string name, string kind) => new()
    {
        Id = Guid.NewGuid(), CaseId = caseId, Name = name, Kind = kind,
        UploadedById = UserId, UploadedByName = Name, UploadedByRole = Role
    };

    private static string SafeName(string value)
    {
        var clean = value.Replace("\0", string.Empty).Trim();
        return clean.Length <= 240 ? clean : clean[..240];
    }

    private async Task<(bool Allowed, bool NotFound)> CheckAccess(Guid caseId, string settingKey)
    {
        var c = await _db.Cases.AsNoTracking().Include(x => x.Assignments).Include(x => x.Type3Workflow).ThenInclude(x => x!.MemberRemarks)
            .SingleOrDefaultAsync(x => x.Id == caseId);
        if (c is null) return (false, true);
        if (c.Type == CaseType.Type1) return (false, false);
        var setting = await _db.SystemSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Key == settingKey);
        var roles = (setting?.Value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (Role != "super-admin" && !roles.Contains(Role, StringComparer.OrdinalIgnoreCase)) return (false, false);
        var canSeeCase = Role is "super-admin" or "proctor" or "coordinator" or "vc"
            || (Role == "female-coordinator" && (c.IsConfidential || c.SubmitterGender == Gender.Female))
            || c.SubmittedByUserId == UserId || c.AssignedToId == UserId || c.Assignments.Any(x => x.IsActive && x.UserId == UserId)
            || c.ForwardedToRole == Role
            || (Role == "dc-member" && c.Type3Workflow != null && c.Type3Workflow.MemberRemarks.Any(x => x.MemberUserId == UserId));
        return (canSeeCase, false);
    }

    private void AddTimeline(Guid caseId, string description) => _db.TimelineEvents.Add(new TimelineEvent
    {
        Id = Guid.NewGuid(), CaseId = caseId, Action = "Investigation Attachment Added", Description = description, User = Name
    });

    private static async Task<bool> HasValidImageSignature(IFormFile file, string contentType)
    {
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header);
        return contentType switch
        {
            "image/jpeg" => read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/png" => read >= 8 && header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            "image/webp" => read >= 12 && System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP",
            _ => false
        };
    }
}

public sealed class AddDriveLinkRequest
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
