using System.Diagnostics;
using System.Runtime.ExceptionServices;
using MediatR;

namespace FlowChat.Shared.Application.DomainEventHandlers.Notifications;

public abstract class DomainEventHandlerBase<TNotification> : IAggregateDomainEventHandler<TNotification>
    where TNotification : INotification
{
    public async Task Handle(TNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            await HandleNotificationAsync(notification, cancellationToken);
        }
        catch (Exception exception)
        {
            await HandleUnexpectedExceptionAsync(notification, exception, cancellationToken);
        }
    }

    protected abstract Task HandleNotificationAsync(TNotification notification, CancellationToken cancellationToken);

    protected virtual Task HandleUnexpectedExceptionAsync(
        TNotification notification,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Preserve the original stack trace when rethrowing from the async handler boundary
        ExceptionDispatchInfo.Capture(exception).Throw();
        throw new UnreachableException();
    }
}
