using PROCTOR.Application.DTOs.Notifications;

namespace PROCTOR.Application.Interfaces;

public interface INotificationService
{
    Task CreateAsync(Guid? userId, string? role, string title, string message, Guid? caseId = null);
    Task<List<NotificationDto>> GetByUserAsync(Guid userId, string role);
    Task MarkAsReadAsync(Guid notificationId);
    Task MarkAllAsReadAsync(Guid userId, string role);
    Task<int> GetUnreadCountAsync(Guid userId, string role);
}
