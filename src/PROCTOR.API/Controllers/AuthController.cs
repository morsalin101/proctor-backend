using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROCTOR.Application.DTOs.Auth;
using PROCTOR.Application.Interfaces;

namespace PROCTOR.API.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        if (!response.Success)
            return Unauthorized(response);

        return Ok(response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var response = await _authService.RefreshTokenAsync(request);
        if (!response.Success)
            return Unauthorized(response);

        return Ok(response);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        var response = await _authService.LogoutAsync(request.RefreshToken);
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }
}

public class LogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
