using PROCTOR.Application.DTOs.Notifications;
using PROCTOR.Application.Interfaces;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Interfaces;

namespace PROCTOR.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IRepository<Notification> _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(IRepository<Notification> notificationRepository, IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task CreateAsync(Guid? userId, string? role, string title, string message, Guid? caseId = null)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Role = role,
            Title = title,
            Message = message,
            IsRead = false,
            CaseId = caseId
        };

        await _notificationRepository.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<List<NotificationDto>> GetByUserAsync(Guid userId, string role)
    {
        var notifications = await _notificationRepository.FindAsync(n =>
            n.UserId == userId || n.Role == role);

        return notifications
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationDto
            {
                Id = n.Id.ToString(),
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CaseId = n.CaseId?.ToString(),
                CreatedAt = n.CreatedAt.ToString("o")
            })
            .ToList();
    }

    public async Task MarkAsReadAsync(Guid notificationId)
    {
        var notification = await _notificationRepository.GetByIdAsync(notificationId);
        if (notification is null) return;

        notification.IsRead = true;
        _notificationRepository.Update(notification);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(Guid userId, string role)
    {
        var unread = await _notificationRepository.FindAsync(n =>
            (n.UserId == userId || n.Role == role) && !n.IsRead);

        foreach (var n in unread)
        {
            n.IsRead = true;
            n.UpdatedAt = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, string role)
    {
        var unread = await _notificationRepository.FindAsync(n =>
            (n.UserId == userId || n.Role == role) && !n.IsRead);

        return unread.Count();
    }
}
