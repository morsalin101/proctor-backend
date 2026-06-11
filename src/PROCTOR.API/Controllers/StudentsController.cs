using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        Contact = s.Contact,
        Email = s.Email,
        Gender = s.Gender.ToKebabCase(),
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

        var existing = await _students.FindAsync(s => s.StudentId == sid);
        if (existing.Any())
            return BadRequest(ApiResponse<StudentDto>.FailResponse("A student with that ID already exists."));

        var student = new Student
        {
            Id = Guid.NewGuid(),
            StudentId = sid,
            Name = request.Name.Trim(),
            Department = request.Department,
            Contact = request.Contact,
            Email = request.Email,
            Gender = string.IsNullOrWhiteSpace(request.Gender) ? Gender.Unspecified : MappingExtensions.ParseEnum<Gender>(request.Gender),
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
}
