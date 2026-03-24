using FlowChat.NotificationService.Application.Contracts.Persistence;
using FlowChat.NotificationService.Domain.Entities;
using FlowChat.Persistence.EntityFrameworkCore;

namespace FlowChat.NotificationService.Persistence.Repositories;

public sealed class NotificationWriteRepository(AppDbContext dbContext)
    : WriteRepositoryBase<Notification>(dbContext), INotificationWriteRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public override async Task<Notification> AddAsync(Notification entity, CancellationToken cancellationToken = default)
    {
        await base.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public override async Task UpdateAsync(Notification entity, CancellationToken cancellationToken = default)
    {
        await base.UpdateAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public override async Task DeleteAsync(Notification entity, CancellationToken cancellationToken = default)
    {
        await base.DeleteAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
