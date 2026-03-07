using FlowChat.Domain.Abstractions;

namespace FlowChat.Application.Abstractions;

public abstract class DomainEventHandlerBase<TDomainEvent> : IDomainEventHandler<TDomainEvent>
    where TDomainEvent : DomainEventBase
{
    public async Task Handle(TDomainEvent notification, CancellationToken cancellationToken)
    {
        await ExecuteAsync(notification, cancellationToken);
    }

    protected abstract Task ExecuteAsync(TDomainEvent notification, CancellationToken cancellationToken);
}
