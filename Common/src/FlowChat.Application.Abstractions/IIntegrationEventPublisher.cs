using FlowChat.Core.Messaging;

namespace FlowChat.Application.Abstractions;

public interface IIntegrationEventPublisher
{
    Task PublishToOutboxAsync<TEvent>(TEvent message, CancellationToken cancellationToken)
        where TEvent : IntegrationEvent;
}
