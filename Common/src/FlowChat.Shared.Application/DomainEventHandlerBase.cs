using FlowChat.Shared.Domain;
using FlowChat.Core.Messaging;

namespace FlowChat.Shared.Application;

public abstract class DomainEventHandlerBase<TDomainEvent, TIntegrationEvent> : IDomainEventHandler<TDomainEvent>
    where TDomainEvent : DomainEventBase
    where TIntegrationEvent : IntegrationEvent
{
    private readonly IOutboxIntegrationEventPublisher _integrationEventPublisher;

    protected DomainEventHandlerBase(IOutboxIntegrationEventPublisher integrationEventPublisher)
    {
        _integrationEventPublisher = integrationEventPublisher
            ?? throw new ArgumentNullException(nameof(integrationEventPublisher));
    }

    public async Task Handle(TDomainEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = MapToIntegrationEvent(notification);
        var envelope = new IntegrationEventEnvelope<TIntegrationEvent>(
            integrationEvent,
            ResolveKafkaKey(notification, integrationEvent));

        await _integrationEventPublisher.Publish(
            envelope,
            cancellationToken);
        await ExecuteAsync(notification, cancellationToken);
    }

    protected abstract TIntegrationEvent MapToIntegrationEvent(TDomainEvent notification);

    protected abstract string ResolveKafkaKey(TDomainEvent notification, TIntegrationEvent integrationEvent);

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

