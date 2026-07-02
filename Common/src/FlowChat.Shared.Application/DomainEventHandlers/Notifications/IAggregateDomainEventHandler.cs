using MediatR;

namespace FlowChat.Shared.Application.DomainEventHandlers.Notifications;

public interface IAggregateDomainEventHandler<TNotification> : INotificationHandler<TNotification>
    where TNotification : INotification
{
}
