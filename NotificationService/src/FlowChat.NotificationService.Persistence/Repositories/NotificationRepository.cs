using FlowChat.NotificationService.Application.Contracts.Persistence;
using FlowChat.NotificationService.Domain.Entities;
using FlowChat.NotificationService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.NotificationService.Persistence.Repositories;

public sealed class NotificationRepository : RepositoryBase<Notification>, INotificationRepository
{
    public NotificationRepository(AppDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<bool> ExistsByUserIdAndTypeAsync(
        Guid userId,
        NotificationType type,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.Notifications.AnyAsync(
            x => x.UserId == userId && x.Type == type,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.Notifications
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedDate)
            .ThenByDescending(x => x.SentAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> GetRecentAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.Notifications
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedDate)
            .ThenByDescending(x => x.SentAtUtc)
            .ToListAsync(cancellationToken);
    }
}
