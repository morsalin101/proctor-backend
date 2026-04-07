using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROCTOR.Application.DTOs.ForwardingRules;
using PROCTOR.Application.Interfaces;

namespace PROCTOR.API.Controllers;

[ApiController]
[Route("api/forwarding-rules")]
[Authorize]
[Produces("application/json")]
public class ForwardingRulesController : ControllerBase
{
    private readonly IForwardingRuleService _service;
    public ForwardingRulesController(IForwardingRuleService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var response = await _service.GetAllAsync();
        return Ok(response);
    }

    [HttpGet("from/{role}")]
    public async Task<IActionResult> GetForRole(string role)
    {
        var response = await _service.GetRulesForRoleAsync(role);
        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "super-admin")]
    public async Task<IActionResult> Create([FromBody] CreateForwardingRuleRequest request)
    {
        var response = await _service.CreateAsync(request);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "super-admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateForwardingRuleRequest request)
    {
        var response = await _service.UpdateAsync(id, request);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "super-admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var response = await _service.DeleteAsync(id);
        return response.Success ? Ok(response) : NotFound(response);
    }
}
