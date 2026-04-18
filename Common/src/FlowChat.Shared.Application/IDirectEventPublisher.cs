using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging;

namespace FlowChat.Shared.Application;

public interface IDirectEventPublisher
{
    Task Publish<TEvent>(TEvent message, CancellationToken cancellationToken)
            where TEvent : IntegrationEvent;
}
