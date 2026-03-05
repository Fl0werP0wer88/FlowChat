using FlowChat.Domain.Abstractions;

namespace FlowChat.Application.Abstractions;

public abstract class DomainEventHandlerBase<TDomainEvent> : IDomainEventHandler<TDomainEvent>
    where TDomainEvent : DomainEventBase
{
    private readonly IIntegrationEventPublisher _integrationEventPublisher;

    protected DomainEventHandlerBase(IIntegrationEventPublisher integrationEventPublisher)
    {
        _integrationEventPublisher = integrationEventPublisher ?? throw new ArgumentNullException(nameof(integrationEventPublisher));
    }

    public async Task Handle(TDomainEvent notification, CancellationToken cancellationToken)
    {
        await _integrationEventPublisher.PublishToOutboxAsync(notification, cancellationToken);
        await ExecuteAsync(notification, cancellationToken);
    }

    protected abstract Task ExecuteAsync(TDomainEvent notification, CancellationToken cancellationToken);
}
