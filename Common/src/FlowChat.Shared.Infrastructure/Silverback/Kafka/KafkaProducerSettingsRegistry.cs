using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging;

namespace FlowChat.Shared.Infrastructure.Silverback.Kafka;

public sealed class KafkaProducerSettingsRegistry
{
    private readonly IReadOnlyDictionary<Type, IKafkaProducerSettingsSection> _map;

    public KafkaProducerSettingsRegistry(IReadOnlyDictionary<Type, IKafkaProducerSettingsSection> map)
    {
        _map = map;
    }

    public IKafkaProducerSettingsSection<TEvent>? Get<TEvent>() where TEvent : IntegrationEvent
        => _map.TryGetValue(typeof(TEvent), out var settings)
            ? (IKafkaProducerSettingsSection<TEvent>)settings
            : null;
}
