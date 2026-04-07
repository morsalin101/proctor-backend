using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROCTOR.Application.DTOs.Articles;
using PROCTOR.Application.Interfaces;

namespace PROCTOR.API.Controllers;

[ApiController]
[Route("api/articles")]
[Authorize]
[Produces("application/json")]
public class ArticlesController : ControllerBase
{
    private readonly IArticleService _articleService;
    public ArticlesController(IArticleService articleService) => _articleService = articleService;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var response = await _articleService.GetAllAsync();
        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "super-admin")]
    public async Task<IActionResult> Create([FromBody] CreateArticleRequest request)
    {
        var response = await _articleService.CreateAsync(request);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "super-admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateArticleRequest request)
    {
        var response = await _articleService.UpdateAsync(id, request);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "super-admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var response = await _articleService.DeleteAsync(id);
        return response.Success ? Ok(response) : NotFound(response);
    }
}
