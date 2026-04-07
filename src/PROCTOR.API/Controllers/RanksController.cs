using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROCTOR.Application.DTOs.Ranks;
using PROCTOR.Application.Interfaces;

namespace PROCTOR.API.Controllers;

[ApiController]
[Route("api/ranks")]
[Authorize]
[Produces("application/json")]
public class RanksController : ControllerBase
{
    private readonly IRankService _rankService;
    public RanksController(IRankService rankService) => _rankService = rankService;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var response = await _rankService.GetAllAsync();
        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "super-admin")]
    public async Task<IActionResult> Create([FromBody] CreateRankRequest request)
    {
        var response = await _rankService.CreateAsync(request);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "super-admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRankRequest request)
    {
        var response = await _rankService.UpdateAsync(id, request);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "super-admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var response = await _rankService.DeleteAsync(id);
        return response.Success ? Ok(response) : NotFound(response);
    }
}
