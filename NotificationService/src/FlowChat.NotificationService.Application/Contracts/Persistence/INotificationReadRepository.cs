using FlowChat.Shared.Application;
using FlowChat.NotificationService.Application.Features.Notifications.Queries.GetNotifications;
using FlowChat.NotificationService.Domain.Enums;

namespace FlowChat.NotificationService.Application.Contracts.Persistence;

public interface INotificationReadRepository : IReadRepository<NotificationDto>
{
    Task<bool> ExistsByUserIdAndTypeAsync(
        Guid userId,
        NotificationType type,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationDto>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationDto>> GetRecentAsync(CancellationToken cancellationToken = default);
}

