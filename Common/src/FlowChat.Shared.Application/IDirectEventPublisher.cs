using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging;

namespace FlowChat.Shared.Application;

public interface IDirectEventPublisher
{
    Task Publish<TEvent, TSection>(TEvent message, CancellationToken cancellationToken)
            where TEvent : IntegrationEvent
            where TSection : IKafkaProducerSettingsSection<TEvent>, new();
}
