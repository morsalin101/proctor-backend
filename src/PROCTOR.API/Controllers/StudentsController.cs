using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;
using PROCTOR.Application.Common;
using PROCTOR.Application.DTOs.Students;
using PROCTOR.Application.Mapping;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
using PROCTOR.Domain.Interfaces;

namespace PROCTOR.API.Controllers;

[ApiController]
[Route("api/students")]
[Authorize]
[Produces("application/json")]
public class StudentsController : ControllerBase
{
    private readonly IRepository<Student> _students;
    private readonly IUnitOfWork _unitOfWork;

    public StudentsController(IRepository<Student> students, IUnitOfWork unitOfWork)
    {
        _students = students;
        _unitOfWork = unitOfWork;
    }

    private static StudentDto ToDto(Student s) => new()
    {
        Id = s.Id.ToString(),
        StudentId = s.StudentId,
        Name = s.Name,
        Department = s.Department,
        Batch = s.Batch,
        Contact = s.Contact,
        Email = s.Email,
        Gender = s.Gender.ToKebabCase(),
        Cgpa = s.Cgpa,
        AdvisorName = s.AdvisorName,
        FatherName = s.FatherName,
        FatherContact = s.FatherContact,
        GuardianContact = s.GuardianContact,
        IsActive = s.IsActive
    };

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var all = await _students.GetAllAsync();
        var dtos = all.OrderBy(s => s.StudentId).Select(ToDto).ToList();
        return Ok(ApiResponse<List<StudentDto>>.SuccessResponse(dtos));
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentStudent()
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId))
            return Unauthorized();

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user is null || !user.IsActive || user.Role != UserRole.Student)
            return Forbid();

        var email = user.Email.ToLower();
        var matches = await _students.FindAsync(s => s.IsActive && s.Email != null && s.Email.ToLower() == email);
        var student = matches.FirstOrDefault();
        if (student is null)
            return NotFound(ApiResponse<StudentDto>.FailResponse("No student directory record is linked to this account."));

        return Ok(ApiResponse<StudentDto>.SuccessResponse(ToDto(student)));
    }

    // Lookup by the university StudentId (e.g. "123") — used to auto-fill the case form.
    [HttpGet("by-student-id/{studentId}")]
    public async Task<IActionResult> GetByStudentId(string studentId)
    {
        var key = (studentId ?? string.Empty).Trim();
        var matches = await _students.FindAsync(s => s.StudentId == key && s.IsActive);
        var student = matches.FirstOrDefault();
        if (student is null)
            return NotFound(ApiResponse<StudentDto>.FailResponse("No student found with that ID."));
        return Ok(ApiResponse<StudentDto>.SuccessResponse(ToDto(student)));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStudentRequest request)
    {
        var sid = (request.StudentId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(sid) || string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse<StudentDto>.FailResponse("Student ID and name are required."));

        if (request.Cgpa is < 0 or > 4)
            return BadRequest(ApiResponse<StudentDto>.FailResponse("CGPA must be between 0 and 4."));

        var existing = await _students.FindAsync(s => s.StudentId == sid);
        if (existing.Any())
            return BadRequest(ApiResponse<StudentDto>.FailResponse("A student with that ID already exists."));

        var student = new Student
        {
            Id = Guid.NewGuid(),
            StudentId = sid,
            Name = request.Name.Trim(),
            Department = request.Department,
            Batch = FormatBatch(request.Batch, request.Department, sid),
            Contact = request.Contact,
            Email = request.Email,
            Gender = string.IsNullOrWhiteSpace(request.Gender) ? Gender.Unspecified : MappingExtensions.ParseEnum<Gender>(request.Gender),
            Cgpa = request.Cgpa,
            AdvisorName = request.AdvisorName,
            FatherName = request.FatherName,
            FatherContact = request.FatherContact,
            GuardianContact = request.GuardianContact,
            IsActive = true
        };
        await _students.AddAsync(student);
        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<StudentDto>.SuccessResponse(ToDto(student), "Student added."));
    }

    private static string FormatBatch(string? batch, string? department, string studentId)
    {
        if (!string.IsNullOrWhiteSpace(batch))
        {
            var normalized = Regex.Replace(batch.Trim().ToUpperInvariant(), @"[^A-Z0-9]+", "_").Trim('_');
            if (!string.IsNullOrWhiteSpace(normalized)) return normalized[..Math.Min(normalized.Length, 64)];
        }

        var departmentCode = Regex.Replace((department ?? string.Empty).ToUpperInvariant(), @"[^A-Z0-9]+", string.Empty);
        if (string.IsNullOrWhiteSpace(departmentCode)) departmentCode = "UNKNOWN";
        departmentCode = departmentCode[..Math.Min(departmentCode.Length, 60)];
        var digits = Regex.Replace(studentId, @"\D", string.Empty);
        var batchNumber = digits.Length >= 3 ? digits[..3] : digits.PadLeft(3, '0');
        return $"{departmentCode}_{batchNumber}";
    }
}
