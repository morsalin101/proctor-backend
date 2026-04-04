using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROCTOR.Application.DTOs.Settings;
using PROCTOR.Application.Interfaces;

namespace PROCTOR.API.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize]
[Produces("application/json")]
public class SystemSettingsController : ControllerBase
{
    private readonly ISystemSettingService _settingService;

    public SystemSettingsController(ISystemSettingService settingService)
    {
        _settingService = settingService;
    }

    private string GetCurrentUserRole() =>
        User.FindFirst("role")?.Value ?? "";

    [HttpGet]
    public async Task<IActionResult> GetAllSettings()
    {
        if (GetCurrentUserRole() != "super-admin")
            return Forbid();

        var response = await _settingService.GetAllSettingsAsync();
        return Ok(response);
    }

    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetSettingsByCategory(string category)
    {
        var response = await _settingService.GetSettingsByCategoryAsync(category);
        return Ok(response);
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> GetSettingByKey(string key)
    {
        var response = await _settingService.GetSettingByKeyAsync(key);
        if (!response.Success)
            return NotFound(response);

        return Ok(response);
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> UpdateSetting(string key, [FromBody] UpdateSettingRequest request)
    {
        if (GetCurrentUserRole() != "super-admin")
            return Forbid();

        var response = await _settingService.UpdateSettingAsync(key, request);
        if (!response.Success)
            return NotFound(response);

        return Ok(response);
    }
}
