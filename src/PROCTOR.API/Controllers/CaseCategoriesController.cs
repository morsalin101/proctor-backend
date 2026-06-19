using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROCTOR.Application.DTOs.CaseCategories;
using PROCTOR.Application.Interfaces;

namespace PROCTOR.API.Controllers;

[ApiController]
[Route("api/case-categories")]
[Authorize]
[Produces("application/json")]
public class CaseCategoriesController : ControllerBase
{
    private readonly ICaseCategoryService _service;

    public CaseCategoriesController(ICaseCategoryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var result = await _service.GetAllAsync(includeInactive);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "super-admin,coordinator,female-coordinator,proctor")]
    public async Task<IActionResult> Create([FromBody] CreateCaseCategoryRequest request)
    {
        var result = await _service.CreateAsync(request);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "super-admin,coordinator,female-coordinator,proctor")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCaseCategoryRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "super-admin,coordinator,female-coordinator,proctor")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }
}
