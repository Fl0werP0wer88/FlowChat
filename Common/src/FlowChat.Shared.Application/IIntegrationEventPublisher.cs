using FlowChat.Core.Messaging;

namespace FlowChat.Shared.Application;

public interface IIntegrationEventPublisher
{
    Task PublishToOutboxAsync<TEvent>(TEvent message, CancellationToken cancellationToken)
        where TEvent : IntegrationEvent;
}

