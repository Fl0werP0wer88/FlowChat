using FlowChat.Core.Messaging;

namespace FlowChat.Shared.Application;

public interface IOutboxIntegrationEventPublisher
{
    Task Publish<TEvent>(TEvent message, CancellationToken cancellationToken)
        where TEvent : IntegrationEvent;
}
