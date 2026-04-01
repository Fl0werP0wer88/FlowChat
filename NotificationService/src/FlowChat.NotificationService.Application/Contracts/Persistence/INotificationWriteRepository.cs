using FlowChat.Shared.Application;
using FlowChat.NotificationService.Domain.Entities;

namespace FlowChat.NotificationService.Application.Contracts.Persistence;

public interface INotificationWriteRepository : IWriteRepository<Notification>
{
}

