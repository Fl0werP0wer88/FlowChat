using FlowChat.NotificationService.Application.Contracts.Persistence;
using FlowChat.NotificationService.Domain.Entities.Notification;
using FlowChat.Shared.Persistance;

namespace FlowChat.NotificationService.Persistence.Repositories;

public sealed class NotificationWriteRepository(AppDbContext dbContext)
    : WriteRepositoryBase<Notification>(dbContext), INotificationWriteRepository
{
}

