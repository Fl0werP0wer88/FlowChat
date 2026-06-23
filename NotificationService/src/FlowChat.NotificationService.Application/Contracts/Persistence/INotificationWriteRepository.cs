using FlowChat.Shared.Application;
using FlowChat.NotificationService.Domain.Entities.Notification;

namespace FlowChat.NotificationService.Application.Contracts.Persistence;

public interface INotificationWriteRepository : IWriteRepository<Notification>
{
}

