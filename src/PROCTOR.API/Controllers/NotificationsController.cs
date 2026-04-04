using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PROCTOR.Application.Common;
using PROCTOR.Application.Interfaces;

namespace PROCTOR.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
[Produces("application/json")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirst("sub")?.Value;
        return sub != null ? Guid.Parse(sub) : Guid.Empty;
    }

    private string GetCurrentUserRole() =>
        User.FindFirst("role")?.Value ?? "";

    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        var notifications = await _notificationService.GetByUserAsync(userId, role);
        return Ok(ApiResponse<object>.SuccessResponse(notifications));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        var count = await _notificationService.GetUnreadCountAsync(userId, role);
        return Ok(ApiResponse<object>.SuccessResponse(new { count }));
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        await _notificationService.MarkAsReadAsync(id);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Notification marked as read."));
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        await _notificationService.MarkAllAsReadAsync(userId, role);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "All notifications marked as read."));
    }
}
