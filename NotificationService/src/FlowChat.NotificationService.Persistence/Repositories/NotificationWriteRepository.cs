using FlowChat.NotificationService.Application.Contracts.Persistence;
using FlowChat.NotificationService.Domain.Entities.Notification;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.NotificationService.Persistence.Repositories;

public sealed class NotificationWriteRepository(AppDbContext dbContext)
    : WriteRepositoryBase<Notification>(dbContext), INotificationWriteRepository
{
    public async Task<Notification?> GetBySourceMessageKeyAsync(
        string sourceMessageKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceMessageKey))
        {
            return null;
        }

        var normalizedKey = sourceMessageKey.Trim();

        return await dbContext.Notifications
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                notification => notification.SourceMessageKey == normalizedKey,
                cancellationToken);
    }
}

