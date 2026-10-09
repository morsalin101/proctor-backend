using PROCTOR.Application.DTOs.Notifications;
using PROCTOR.Application.Interfaces;
using PROCTOR.Domain.Entities;
using PROCTOR.Domain.Enums;
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
        var notifications = CollapseLegacyForwardDuplicates(
            await _notificationRepository.FindAsync(n => n.UserId == userId || n.Role == role));

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

    public async Task<List<NotificationDto>> GetByCaseForUserAsync(Guid caseId, Guid userId, string role)
    {
        var notifications = CollapseLegacyForwardDuplicates(
            await _notificationRepository.FindAsync(n =>
                n.CaseId == caseId && (n.UserId == userId || n.Role == role)));

        return notifications
            .OrderByDescending(n => n.CreatedAt)
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
        // Include read rows before collapsing so a read role broadcast can still be paired
        // with its legacy user-specific duplicate (and vice versa).
        var visible = CollapseLegacyForwardDuplicates(
            await _notificationRepository.FindAsync(n => n.UserId == userId || n.Role == role));

        return visible.Count(n => !n.IsRead);
    }

    public async Task<NotificationCategoryCountsDto> GetUnreadCountsByCategoryAsync(Guid userId, string role)
    {
        var unread = CollapseLegacyForwardDuplicates(
                await _notificationRepository.FindAsync(n => n.UserId == userId || n.Role == role))
            .Where(n => !n.IsRead)
            .ToList();

        var result = new NotificationCategoryCountsDto { Total = unread.Count };

        var caseIds = unread.Where(n => n.CaseId.HasValue).Select(n => n.CaseId!.Value).Distinct().ToList();
        if (caseIds.Count == 0) return result;

        var cases = (await _unitOfWork.Cases.FindAsync(c => caseIds.Contains(c.Id))).ToList();
        var typeById = cases.ToDictionary(c => c.Id, c => c.Type);

        foreach (var n in unread)
        {
            if (!n.CaseId.HasValue || !typeById.TryGetValue(n.CaseId.Value, out var t)) continue;
            var caseEntity = cases.First(c => c.Id == n.CaseId.Value);
            if (caseEntity.IsConfidential || t == CaseType.Confidential)
            {
                result.Confidential++;
                continue;
            }
            switch (t)
            {
                case CaseType.Type1: result.Type1++; break;
                case CaseType.Type2: result.Type2++; break;
                case CaseType.Confidential: result.Confidential++; break;
            }
        }

        return result;
    }

    /// <summary>
    /// Older forwarding code created a role-wide "Case Forwarded to You" row and a direct
    /// "Case Assigned to You" row for the selected person. That person matches both rows.
    /// Keep their user-specific row and suppress only the matching role row; other members
    /// of the role do not receive the direct row, so their broadcast remains visible.
    /// </summary>
    private static List<Notification> CollapseLegacyForwardDuplicates(IEnumerable<Notification> source)
    {
        var notifications = source.ToList();
        var directAssignments = notifications
            .Where(n => n.UserId.HasValue
                && n.CaseId.HasValue
                && n.Title == "Case Assigned to You")
            .ToList();

        return notifications.Where(notification =>
        {
            if (notification.UserId.HasValue
                || !notification.CaseId.HasValue
                || notification.Title != "Case Forwarded to You")
                return true;

            return !directAssignments.Any(direct =>
                direct.CaseId == notification.CaseId
                && Math.Abs((direct.CreatedAt - notification.CreatedAt).TotalSeconds) <= 60);
        }).ToList();
    }
}
