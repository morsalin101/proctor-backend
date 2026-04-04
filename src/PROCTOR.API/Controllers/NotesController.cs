using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Notes;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Interfaces;

namespace PROCTOR.API.Controllers;

[ApiController]
[Route("api/cases/{caseId:guid}/notes")]
[Authorize]
[Produces("application/json")]
public class NotesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public NotesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private string GetCurrentUserName() =>
        User.FindFirst("name")?.Value ?? "Unknown";

    [HttpPost]
    public async Task<IActionResult> AddNote(Guid caseId, [FromBody] CreateNoteRequest request)
    {
        var caseEntity = await _unitOfWork.Cases.GetByIdAsync(caseId);
        if (caseEntity == null)
            return NotFound(ApiResponse<object>.FailResponse("Case not found"));

        var author = GetCurrentUserName();

        var note = new Note
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            Content = request.Content,
            Author = author,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        caseEntity.Notes.Add(note);

        var timelineEvent = new TimelineEvent
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            Action = "Note Added",
            Description = $"Note added by {author}",
            User = author,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        caseEntity.TimelineEvents.Add(timelineEvent);

        await _unitOfWork.SaveChangesAsync();

        var noteDto = new NoteDto
        {
            Id = note.Id.ToString(),
            Content = note.Content,
            Author = note.Author,
            CreatedDate = note.CreatedAt.ToString("yyyy-MM-dd")
        };

        return Ok(ApiResponse<NoteDto>.SuccessResponse(noteDto, "Note added successfully"));
    }
}
