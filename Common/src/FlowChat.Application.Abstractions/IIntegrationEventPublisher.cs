using FlowChat.Messaging.Contracts;

namespace FlowChat.Application.Abstractions;

public interface IIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(IntegrationEventEnvelope<TEvent> message, CancellationToken cancellationToken)
        where TEvent : IntegrationEvent;
}
