using FlowChat.NotificationService.Application.Contracts.Persistence;
using FlowChat.NotificationService.Application.Features.Notification.Queries.GetNotifications;
using FlowChat.NotificationService.Domain.Entities.Notification;
using FlowChat.NotificationService.Domain.Enums;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FlowChat.NotificationService.Persistence.Repositories;

public sealed class NotificationReadRepository(AppDbContext dbContext)
    : ReadRepositoryBase<Notification, NotificationDto>(dbContext), INotificationReadRepository
{
    private static readonly Expression<Func<Notification, NotificationDto>> NotificationDtoProjection = x => new(
        x.Id.Value,
        x.UserId.Value,
        x.Email.Value,
        x.DisplayName,
        x.Body,
        x.Type,
        x.Status,
        x.ProviderMessageId,
        x.FailureReason,
        x.SourceMessageKey,
        x.SentAtUtc == null ? null : x.SentAtUtc.Value,
        x.CreatedAtUtc.UtcDateTime);

    protected override Expression<Func<Notification, NotificationDto>> MapToDto => NotificationDtoProjection;

    public async Task<bool> ExistsByUserIdAndTypeAsync(
        Guid userId,
        NotificationType type,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<Notification>().AnyAsync(
            x => x.UserId.Value == userId && x.Type == type,
            cancellationToken);
    }

    public async Task<bool> ExistsBySourceMessageKeyAsync(
        string sourceMessageKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceMessageKey))
        {
            return false;
        }

        var normalizedKey = sourceMessageKey.Trim();

        return await DbContext.Set<Notification>().AnyAsync(
            x => x.SourceMessageKey == normalizedKey,
            cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.UserId.Value == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.SentAtUtc)
            .Select(MapToDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetRecentAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.SentAtUtc)
            .Select(MapToDto)
            .ToListAsync(cancellationToken);
    }
}

