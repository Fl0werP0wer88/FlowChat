using FlowChat.Shared.Domain;
using FlowChat.Core.Messaging;

namespace FlowChat.Shared.Application;

public abstract class DomainEventHandlerBase<TDomainEvent, TIntegrationEvent> : IDomainEventHandler<TDomainEvent>
    where TDomainEvent : DomainEventBase
    where TIntegrationEvent : IntegrationEvent
{
    private readonly IIntegrationEventPublisher _integrationEventPublisher;

    protected DomainEventHandlerBase(IIntegrationEventPublisher integrationEventPublisher)
    {
        _integrationEventPublisher = integrationEventPublisher
            ?? throw new ArgumentNullException(nameof(integrationEventPublisher));
    }

    public async Task Handle(TDomainEvent notification, CancellationToken cancellationToken)
    {
        await _integrationEventPublisher.PublishToOutboxAsync(
            MapToIntegrationEvent(notification),
            cancellationToken);
        await ExecuteAsync(notification, cancellationToken);
    }

    protected abstract TIntegrationEvent MapToIntegrationEvent(TDomainEvent notification);

    protected abstract Task ExecuteAsync(TDomainEvent notification, CancellationToken cancellationToken);
}

public abstract class DomainEventHandlerBase<TDomainEvent> : IDomainEventHandler<TDomainEvent>
    where TDomainEvent : DomainEventBase
{
    public async Task Handle(TDomainEvent notification, CancellationToken cancellationToken)
    {
        await ExecuteAsync(notification, cancellationToken);
    }

    protected abstract Task ExecuteAsync(TDomainEvent notification, CancellationToken cancellationToken);
}

