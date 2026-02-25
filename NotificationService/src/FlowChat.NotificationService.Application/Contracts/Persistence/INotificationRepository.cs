using FlowChat.NotificationService.Domain.Entities;
using FlowChat.NotificationService.Domain.Enums;

namespace FlowChat.NotificationService.Application.Contracts.Persistence;

public interface INotificationRepository : IAsyncRepository<Notification>
{
    Task<bool> ExistsByUserIdAndTypeAsync(
        Guid userId,
        NotificationType type,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> GetRecentAsync(CancellationToken cancellationToken = default);
}
