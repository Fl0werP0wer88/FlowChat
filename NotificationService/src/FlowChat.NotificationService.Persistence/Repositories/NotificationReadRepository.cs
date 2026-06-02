using FlowChat.NotificationService.Application.Contracts.Persistence;
using FlowChat.NotificationService.Application.Features.Notification.Queries.GetNotifications;
using FlowChat.NotificationService.Domain.Enums;
using FlowChat.NotificationService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FlowChat.NotificationService.Persistence.Repositories;

public sealed class NotificationReadRepository(AppDbContext dbContext) : INotificationReadRepository
{
    private static readonly Expression<Func<NotificationReadEntity, NotificationDto>> NotificationDtoProjection = x => new(
        x.Id,
        x.UserId,
        x.Email,
        x.DisplayName,
        x.Body,
        x.Type,
        x.Status,
        x.ProviderMessageId,
        x.FailureReason,
        x.SourceMessageKey,
        x.SentAtUtc,
        x.CreatedAtUtc);

    public async Task<NotificationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Query()
            .Where(x => x.Id == id)
            .Select(NotificationDtoProjection)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Query()
            .Select(NotificationDtoProjection)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByUserIdAndTypeAsync(
        Guid userId,
        NotificationType type,
        CancellationToken cancellationToken = default)
    {
        return await Query().AnyAsync(
            x => x.UserId == userId && x.Type == type,
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

        return await Query().AnyAsync(
            x => x.SourceMessageKey == normalizedKey,
            cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await Query()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.SentAtUtc)
            .Select(NotificationDtoProjection)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetRecentAsync(CancellationToken cancellationToken = default)
    {
        return await Query()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.SentAtUtc)
            .Select(NotificationDtoProjection)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<NotificationReadEntity> Query() => dbContext.NotificationReads.AsNoTracking();
}

