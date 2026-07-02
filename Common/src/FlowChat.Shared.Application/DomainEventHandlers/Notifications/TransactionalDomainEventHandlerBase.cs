using MediatR;

namespace FlowChat.Shared.Application.DomainEventHandlers.Notifications;

public abstract class TransactionalDomainEventHandlerBase<TNotification> : DomainEventHandlerBase<TNotification>
    where TNotification : INotification
{
    private readonly IUnitOfWork _unitOfWork;

    protected TransactionalDomainEventHandlerBase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    protected sealed override Task HandleNotificationAsync(TNotification notification, CancellationToken cancellationToken)
        => _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                await HandleInTransactionAsync(notification, token);

                if (_unitOfWork is IConsumedOffsetCommitter offsetCommitter)
                {
                    await offsetCommitter.CommitConsumedOffsetsAsync(token);
                }

                return Unit.Value;
            },
            cancellationToken);

    protected abstract Task HandleInTransactionAsync(TNotification notification, CancellationToken cancellationToken);
}
