using FlowChat.Messaging.Contracts;

namespace FlowChat.AuthService.Application.Contracts.Infrastructure;

public interface IIntegrationEventPublisher<TEvent> where TEvent : class
{
    Task PublishAsync(TEvent message, CancellationToken cancellationToken);
}
