using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging;

namespace FlowChat.Shared.Application;

public interface IOutboxIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(IntegrationEventEnvelope<TEvent> message, CancellationToken cancellationToken)
            where TEvent : IntegrationEvent;
}
