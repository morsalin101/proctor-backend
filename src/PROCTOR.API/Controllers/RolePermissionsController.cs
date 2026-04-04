using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROCTOR.Application.DTOs.Permissions;
using PROCTOR.Application.Interfaces;

namespace PROCTOR.API.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
[Produces("application/json")]
public class RolePermissionsController : ControllerBase
{
    private readonly IRolePermissionService _rolePermissionService;

    public RolePermissionsController(IRolePermissionService rolePermissionService)
    {
        _rolePermissionService = rolePermissionService;
    }

    private string GetCurrentUserRole() =>
        User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value ?? "";

    [HttpGet]
    public async Task<IActionResult> GetAllRoles()
    {
        var response = await _rolePermissionService.GetAllRolesAsync();
        return Ok(response);
    }

    [HttpGet("{id:guid}/permissions")]
    public async Task<IActionResult> GetRolePermissions(Guid id)
    {
        var response = await _rolePermissionService.GetRolePermissionsAsync(id);
        if (!response.Success)
            return NotFound(response);

        return Ok(response);
    }

    [HttpPut("{id:guid}/permissions")]
    public async Task<IActionResult> UpdateRolePermissions(Guid id, [FromBody] UpdateMenuPermissionsRequest request)
    {
        if (GetCurrentUserRole() != "super-admin")
            return Forbid();

        var response = await _rolePermissionService.UpdateRolePermissionsAsync(id, request);
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpGet("by-name/{roleName}")]
    public async Task<IActionResult> GetRoleByName(string roleName)
    {
        var response = await _rolePermissionService.GetRolePermissionsByNameAsync(roleName);
        if (!response.Success)
            return NotFound(response);

        return Ok(response);
    }

    [HttpPut("by-name/{roleName}/permissions")]
    public async Task<IActionResult> UpdateRolePermissionsByName(string roleName, [FromBody] UpdateMenuPermissionsRequest request)
    {
        if (GetCurrentUserRole() != "super-admin")
            return Forbid();

        var response = await _rolePermissionService.UpdateRolePermissionsByNameAsync(roleName, request);
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }
}
