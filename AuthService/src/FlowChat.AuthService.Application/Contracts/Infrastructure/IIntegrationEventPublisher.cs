using FlowChat.Messaging.Contracts;

namespace FlowChat.AuthService.Application.Contracts.Infrastructure;

public interface IIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(IntegrationEventEnvelope<TEvent> message, CancellationToken cancellationToken)
        where TEvent : IntegrationEvent;
}
