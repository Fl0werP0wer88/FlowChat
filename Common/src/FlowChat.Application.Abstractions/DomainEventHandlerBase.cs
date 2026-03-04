using FlowChat.Domain.Abstractions;

namespace FlowChat.Application.Abstractions;

public abstract class DomainEventHandlerBase<TDomainEvent> : IDomainEventHandler<TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    public async Task Handle(TDomainEvent notification, CancellationToken cancellationToken)
    {
        await PublishToOutboxAsync(notification,cancellationToken);
        await ExecuteAsync(notification, cancellationToken);
    }

    protected abstract Task ExecuteAsync(TDomainEvent notification, CancellationToken cancellationToken);

    protected abstract Task PublishToOutboxAsync(TDomainEvent notification, CancellationToken cancellationToken);
}
