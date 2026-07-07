using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteAssignment.Application.Notification;

public sealed record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    bool IsRead,
    DateTimeOffset CreatedAt
);

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> GetUserNotificationsAsync(Guid userId, CancellationToken ct = default);
    Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    
    // Internal method called by jobs to create a notification
    Task CreateNotificationAsync(Guid userId, string title, string message, CancellationToken ct = default);
}
