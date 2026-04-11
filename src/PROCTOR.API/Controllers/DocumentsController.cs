using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Documents;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Domain.Interfaces;

namespace PROCTOR.API.Controllers;

[ApiController]
[Route("api/cases/{caseId:guid}/documents")]
[Authorize]
[Produces("application/json")]
public class DocumentsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _env;

    public DocumentsController(IUnitOfWork unitOfWork, IWebHostEnvironment env)
    {
        _unitOfWork = unitOfWork;
        _env = env;
    }

    private string GetCurrentUserName() =>
        User.FindFirst("name")?.Value ?? "Unknown";

    private string GetCurrentUserRole() =>
        User.FindFirst("role")?.Value ?? "";

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB
    public async Task<IActionResult> AddDocument(Guid caseId, [FromForm] IFormFile file, [FromForm] string? name)
    {
        var caseEntity = await _unitOfWork.Cases.GetByIdAsync(caseId);
        if (caseEntity == null)
            return NotFound(ApiResponse<object>.FailResponse("Case not found"));

        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.FailResponse("No file uploaded"));

        var uploadedBy = GetCurrentUserName();

        // Determine document type from content type
        var documentType = file.ContentType switch
        {
            var ct when ct.StartsWith("image/") => DocumentType.Image,
            var ct when ct.StartsWith("video/") => DocumentType.Video,
            "application/pdf" => DocumentType.Pdf,
            _ => DocumentType.Other
        };

        // Save file to disk
        var uploadsPath = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsPath);
        var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsPath, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativeUrl = $"/uploads/{uniqueFileName}";

        var document = new Document
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            Name = name ?? file.FileName,
            Type = documentType,
            Url = relativeUrl,
            UploadedBy = uploadedBy,
            UploadedByRole = GetCurrentUserRole(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _unitOfWork.Add(document);

        _unitOfWork.Add(new TimelineEvent
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            Action = "Document Added",
            Description = $"Document '{document.Name}' uploaded by {uploadedBy}",
            User = uploadedBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync();

        var documentDto = new DocumentDto
        {
            Id = document.Id.ToString(),
            Name = document.Name,
            Type = document.Type.ToString().ToLower(),
            Url = document.Url,
            UploadedBy = document.UploadedBy,
            UploadedByRole = document.UploadedByRole,
            UploadedDate = document.CreatedAt.ToString("o")
        };

        return Ok(ApiResponse<DocumentDto>.SuccessResponse(documentDto, "Document added successfully"));
    }

    [HttpDelete("{documentId:guid}")]
    public async Task<IActionResult> DeleteDocument(Guid caseId, Guid documentId)
    {
        var caseEntity = await _unitOfWork.Cases.GetByIdWithDetailsAsync(caseId);
        if (caseEntity == null)
            return NotFound(ApiResponse<object>.FailResponse("Case not found"));

        var doc = caseEntity.Documents.FirstOrDefault(d => d.Id == documentId);
        if (doc == null)
            return NotFound(ApiResponse<object>.FailResponse("Document not found"));

        // Delete file from disk (best-effort)
        try
        {
            var diskPath = Path.Combine(_env.ContentRootPath, "wwwroot", doc.Url.TrimStart('/'));
            if (System.IO.File.Exists(diskPath)) System.IO.File.Delete(diskPath);
        }
        catch { /* swallow disk errors so DB delete still proceeds */ }

        _unitOfWork.Remove(doc);

        _unitOfWork.Add(new TimelineEvent
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            Action = "Document Removed",
            Description = $"Document '{doc.Name}' was removed by {GetCurrentUserName()}",
            User = GetCurrentUserName(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Document deleted"));
    }
}
