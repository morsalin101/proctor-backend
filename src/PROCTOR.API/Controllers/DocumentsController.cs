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

    public DocumentsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private string GetCurrentUserName() =>
        User.FindFirst("name")?.Value ?? "Unknown";

    [HttpPost]
    public async Task<IActionResult> AddDocument(Guid caseId, [FromBody] AddDocumentRequest request)
    {
        var caseEntity = await _unitOfWork.Cases.GetByIdAsync(caseId);
        if (caseEntity == null)
            return NotFound(ApiResponse<object>.FailResponse("Case not found"));

        var uploadedBy = GetCurrentUserName();

        if (!Enum.TryParse<DocumentType>(request.Type, true, out var documentType))
            documentType = DocumentType.Other;

        var document = new Document
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            Name = request.Name,
            Type = documentType,
            Url = request.Url,
            UploadedBy = uploadedBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        caseEntity.Documents.Add(document);

        var timelineEvent = new TimelineEvent
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            Action = "Document Added",
            Description = $"Document '{request.Name}' uploaded by {uploadedBy}",
            User = uploadedBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        caseEntity.TimelineEvents.Add(timelineEvent);

        await _unitOfWork.SaveChangesAsync();

        var documentDto = new DocumentDto
        {
            Id = document.Id.ToString(),
            Name = document.Name,
            Type = document.Type.ToString().ToLower(),
            Url = document.Url,
            UploadedBy = document.UploadedBy,
            UploadedDate = document.CreatedAt.ToString("yyyy-MM-dd")
        };

        return Ok(ApiResponse<DocumentDto>.SuccessResponse(documentDto, "Document added successfully"));
    }
}

public class AddDocumentRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
